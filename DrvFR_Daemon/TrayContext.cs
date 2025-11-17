using System;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DrvFR_Daemon
{
    class TrayContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private Thread workerThread;
        private volatile bool stopRequested;

        private Logger logger;
        private string logFile = "drvfr_daemon.log";

        private string pipeName = "kktpipe";

        private DateTime start;

        public TrayContext()
        {
            // иконки можно добавить в ресурсы (здесь для XP допустимы .ico 16x16)
            trayIcon = new NotifyIcon()
            {
//                Icon = SystemIcons.Application,
                Icon = Properties.Resources.logo,
                Visible = true,
                Text = "KKT Daemon (не запущен)"
            };

            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add("Открыть лог", (s, e) => OpenLog());
//            menu.MenuItems.Add("Запустить демон", (s, e) => StartDaemon());
//            menu.MenuItems.Add("Остановить демон", (s, e) => StopDaemon());
            menu.MenuItems.Add("Выход", (s, e) => Exit());
            trayIcon.ContextMenu = menu;

            logger = new Logger(logFile);

            StartDaemon();
        }
        
        private void OpenLog()
        {
            logger.OpenFile();
        }

        private void Exit()
        {
            StopDaemon();
//            trayIcon.Visible = false;
            trayIcon.Dispose();
            logger.Write("Выход из программы...");
            Application.Exit();
        }
        
        private void StartDaemon()
        {
            stopRequested = false;
            workerThread = new Thread(DaemonLoop);
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        private void RestartDaemon()
        {
            StopDaemon();
            StartDaemon();
        }

        private void StopDaemon()
        {
            stopRequested = true;
            if (workerThread != null && workerThread.IsAlive)
            {
                workerThread.Join(500);
                logger.Write("KKT Daemon остановлен...");
            }
        }

        private void DaemonLoop()
        {
            DK dk1 = new DK();

            if (dk1.DrvInit() == false)
            {
                logger.Write("Драйвер DrvFR не найден! Программа будет закрыта.");

                MessageBox.Show("Драйвер DrvFR не найден!\nПрограмма будет закрыта.",
                    "KKT Daemon", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            if (dk1.GetShortECRStatus() == false)
            {
                logger.Write("Касса не подключена! Программа будет закрыта.");

                MessageBox.Show("Касса не подключена!\nПрограмма будет закрыта.",
                    "KKT Daemon", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Exit();
            }

            logger.Separator();
            logger.Separator();

            string version = dk1.getDriverVersion();
            logger.Write(string.Format("Драйвер DrvFR {0} загружен...", version));

            trayIcon.Text = "KKT Daemon (работает)";
            logger.Write("KKT Daemon запущен...");
            logger.Separator();

            while (!stopRequested)
            {
                NamedPipeServerStream pipe = null;
                StreamReader reader = null;
                StreamWriter writer = null;

                try
                {
                    pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.None, 512, 512);

                    pipe.WaitForConnection();
/*
                    var ar = pipe.BeginWaitForConnection(null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(900))   // ждём 300 мс
                    {
                        pipe.Dispose();                       // никого нет → закрываем пайп
                        continue;                            // новая попытка
                    }

                    pipe.EndWaitForConnection(ar);          // клиент подключился
*/
                    reader = new StreamReader(pipe, Encoding.UTF8);
                    writer = new StreamWriter(pipe, Encoding.UTF8);
                    writer.AutoFlush = true;

                    string line = reader.ReadLine();
                    if (line == null)
                        continue;

                    string[] parts = line.Split(' ');

                    if (parts.Length < 2)
                    {
                        writer.WriteLine(FormatResult(false, "01"));    // Код ошибки: недостаточно аргументов
                        writer.Flush();
                        continue;
                    }

                    string commandCode = parts[0];
                    string[] commandParams = (parts.Length > 1) ? parts[1].Split(',') : new string[0];

                    string password = commandParams[0];
                    dk1.SetPassword(int.Parse(password)); // установить пароль

                    string result = ExecuteCommand(dk1, commandCode, commandParams);
                    writer.WriteLine(result);
                    writer.Flush();
                }
                catch (Exception ex)
                {
                    logger.Write("Ошибка pipe: " + ex.Message);
                }
                finally
                {
                    if (writer != null)
                    {
                        writer.Close();
                        writer.Dispose();
                    }
                    if (reader != null) //reader.Dispose();
                    {
                        reader.Close();
                        reader.Dispose();
                    }
                    if (pipe != null)
                    {
                        if (pipe.IsConnected)
                            pipe.Disconnect();
                        pipe.Dispose();
                    }

//                  System.Threading.Thread.Sleep(200);
                }
            }
            trayIcon.Text = "KKT Daemon (остановлен)";
        }

        private string ExecuteCommand(DK dk1, string commandCode, string[] commandParams)
        {            
            start = DateTime.Now;
            logger.Write(string.Format("Ком: {0} {1}", commandCode, string.Join(",", commandParams)));

            try
            {
                switch (commandCode)
                {
                    case "$666":
//                        dk1.ShowProperties();
                        dk1.AboutBox();
                        break;

                    case "$01":
                        // TestDriver(dk1); // Тест драйвера
                        break;

                    case "$12":  // Печать широкой строки (30)
                        // commandParams[1] = 2;
                        string ws = commandParams[2]; // строка
                        return FormatResult(dk1.PrintWideString(ws), commandCode, dk1.ResultCode.ToString());

                    case "$17": // Печать строки (30)
                        // calldrvi $17 30,2,С_инструкцией_по_эксплуатации 
                        // calldrvi $17 30,3,Выр._отд_1(тов.)____11335.00 
                        // commandParams[1] = 2;
                        string s = commandParams[2]; // строка
                        return FormatResult(dk1.PrintString(s), commandCode, dk1.ResultCode.ToString());

                    case "$C1":  // Печать графики (??)
                        return FormatResult(dk1.Draw(), commandCode, dk1.ResultCode.ToString());

                    case "$28":  // Открытие ящика (29)
                        return FormatResult(dk1.OpenDrawer(), commandCode, dk1.ResultCode.ToString());

                    case "$41": // Z-отчет (29)
                        return FormatResult(dk1.ZReport(), commandCode, dk1.ResultCode.ToString());

                    case "$40": // X-отчет (29)
                        return FormatResult(dk1.XReport(), commandCode, dk1.ResultCode.ToString());


                    case "$80":  // Продажа FNOperation (??)
                    case "$82":  // Возврат продажи FNOperation (??)
                        // calldrvi $80 1,4000,255000,1,0,0,0,0,ОБОИ_GRANDECO_69601_ALLINA_1.06X -- нал
                        // calldrvi $80 1,1000,0,1,0,0,0,0,4.00_ск100%_ПАКЕТ_МАЙКА_ПНД_30* -- нал
                        // calldrvi $80 1,1000,299500,8,0,0,0,0,ОБОИ_GRANDECO_74103_BOHEME_1.06X -- безнал

                        // ПРОВЕРИТЬ ЧТО ОТКРЫТА СМЕНА ИНАЧЕ ОТКРЫТЬ
                        // dk1.OpenSession();

                        double quantity = double.Parse(commandParams[1]) / 1000;    // делим кол-во на 1000
                        decimal price = decimal.Parse(commandParams[2]) / 100;      // делим на 100, чтобы получить рубли и копейки
                        int department = int.Parse(commandParams[3]);
                        // commandParams[4] = 2;
                        // commandParams[5] = 2;
                        // commandParams[6] = 2;
                        // commandParams[7] = 2;
                        string tovar = commandParams[8];

                        if (commandCode == "$80")
                        {
                            return FormatResult(dk1.Sale(quantity, price, department, tovar), commandCode, dk1.ResultCode.ToString());
                        }
                        else if (commandCode == "$82")
                        {
                            return FormatResult(dk1.ReturnSale(quantity, price, department, tovar), commandCode, dk1.ResultCode.ToString());
                        }
                        break;

                    case "$89":  // Подытог чека
                        // calldrvi $89 1
                        if (dk1.CheckSubTotal() == false)
                        {
                            return FormatResult(false, commandCode, dk1.ResultCode.ToString());
                        }

                        string sumkop = (decimal.Parse(dk1.ResultData[0]) * 100).ToString(); // умножаем на 100, чтобы получить копейки
                        return FormatResult(true, commandCode, sumkop);        // СУММА В КОПЕЙКАХ ДОЛЖНА БЫТЬ

                    case "$85":   // Закрытие чека (??)
                        // calldrvi $85 1,0,968500 -- банк
                        // calldrvi $85 1,1065000 -- наличка
                        decimal sum;
                        bool cash;

                        if (commandParams[1] == "0")    // если это безнал, ожидаем сумму в commandParams[2]
                        {
                            cash = false;
                            sum = decimal.Parse(commandParams[2]);
                        }
                        else                           // иначе это нал, ожидаем сумму в commandParams[1]
                        {
                            cash = true;
                            sum = decimal.Parse(commandParams[1]);
                        }

                        sum = sum / 100;    // делим на 100, чтобы получить рубли и копейки

                        return FormatResult(dk1.FNCloseCheckEx(sum, cash), commandCode, dk1.ResultCode.ToString());

                    case "$88":  // Аннулирование чека
                        return FormatResult(dk1.CancelCheck(), commandCode, dk1.ResultCode.ToString());

                    case "$25":  // Отрезка чека (30)
                        // commandParams[0] = 1;
                        return FormatResult(dk1.CutCheck(), commandCode, dk1.ResultCode.ToString());

                    case "$B0":  // Возобновление печати (29)
                        return FormatResult(dk1.ContinuePrint(), commandCode, dk1.ResultCode.ToString());

                    case "$11":  // Запрос состояния ФР (29)

                        if (dk1.GetECRStatus() == false)
                        {
                            return FormatResult(false, commandCode, dk1.ResultCode.ToString());
                        }

                        // 8й и 10й надо вернуть
                        return FormatResult(true, commandCode, "1", "2", "3", "4", "5", "6", dk1.ResultData[1], "8", dk1.ResultData[0], "10", "11", "12", "13", "14", "15",
                                             "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29"/*,"30","31","32","33","34","35"*/);

                    case "$1A":   // Запрос регистра ФР (29)
                        // calldrvi $1A 29,149 -- 121 ... 159
                        int reg = int.Parse(commandParams[1]);

                        if (dk1.GetCashReg(reg) == false)
                        {
                            return FormatResult(false, commandCode, dk1.ResultCode.ToString());
                        }

                        string sumkop1 = (decimal.Parse(dk1.ResultData[0]) * 100).ToString(); // умножаем на 100, чтобы получить копейки
                        return FormatResult(true, commandCode, sumkop1);

                    case "$2D":  // Запрос структуры таблицы (30)
                        int table = int.Parse(commandParams[1]);
                        int field, row;

                        if (dk1.GetTableStruct(table) == false)
                        {
                            return FormatResult(false, commandCode, dk1.ResultCode.ToString());
                        }

                        //                      return FormatResult(true, dk1.ResultData[0], dk1.ResultData[1], dk1.ResultData[2]);
                        return FormatResult(true, commandCode);

                    case "$2E":  // Запрос структуры поля (30)
                        table = int.Parse(commandParams[1]);
                        field = int.Parse(commandParams[2]);
                        if (dk1.GetFieldStruct(table, field) == false)
                        {
                            return FormatResult(false, commandCode, dk1.ResultCode.ToString());
                        }

                        //                      return FormatResult(true, dk1.ResultData[0], dk1.ResultData[1], dk1.ResultData[2]);
                        return FormatResult(true, commandCode);

                    case "$1F":   // Чтение таблицы (30)
                        table = int.Parse(commandParams[1]);
                        row = int.Parse(commandParams[2]);
                        field = int.Parse(commandParams[3]);

                        if (dk1.ReadTable(table, row, field) == false)
                        {
                            return FormatResult(false, commandCode, dk1.ResultCode.ToString());
                        }

                        //                      return FormatResult(true, dk1.ResultData[0]);
                        return FormatResult(true, commandCode);

                    case "$1E":  // Запись таблицы (30)
                        table = int.Parse(commandParams[1]);
                        row = int.Parse(commandParams[2]);
                        field = int.Parse(commandParams[3]);
                        string value = commandParams[4];

                        return FormatResult(dk1.WriteTable(table, row, field, value), commandCode, dk1.ResultCode.ToString());

                    default:
                        return FormatResult(false, "88", "03");
                }
            }
            catch (Exception /*ex*/)
            {
                //                Console.WriteLine("Ошибка: " + ex.Message);
                return FormatResult(false, "88", "99"); // Общий код ошибки
            }

            return FormatResult(false, "88"); // Все ОК
        }

        // вывод результата и формат ответа
        private string FormatResult(bool returnStatus, string commandCode, params string[] ret)
        {
            commandCode = commandCode.Substring(1); // отрезаем $ из команды

            DateTime end = DateTime.Now;
            TimeSpan dur = end - start;

            logger.Write(string.Format("Отв: {0} {1}", commandCode, string.Join(",", ret)));
            logger.Write(string.Format("Всего: {0:F3} сек", dur.TotalSeconds));
            logger.Separator();
                        
            // 88 - возврат исполненной код команды
            if (returnStatus == false)  // все плохо, выводим ошибку
            {
                return string.Format("1 {0},{1}", commandCode, string.Join(",", ret));  // ош_низ_уров команда,ош_выс_уров,продавец
            }
            else
            {
                if (ret.Length > 0)
                    return string.Format("0 {0},00,30,{1}", commandCode, string.Join(",", ret)); // ош_низ_уров команда,ош_выс_уров,продавец
                else
                    return string.Format("0 {0},00,30", commandCode); // ош_низ_уров команда,ош_выс_уров,продавец
            }
        }
    }
}
