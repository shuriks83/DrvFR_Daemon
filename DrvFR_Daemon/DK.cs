using System;
using DrvFRLib;

namespace DrvFR_Daemon
{
    class DK
    {
        DrvFR Drv;

        public int ResultCode;
        public string ResultDescription = "";

        public string[] ResultData;

        public DK()
        {
            // Инициализация массива с длиной 2
            ResultData = new string[5];
        }

        private void SetCode()
        {
            ResultCode = Drv.ResultCode;
            ResultDescription = Drv.ResultCodeDescription;
        }

        public bool DrvInit()
        {
            Drv = new DrvFR();
            if (Drv == null) return false;

            //            Drv.CheckConnection()
            return true;

            /*
            Drv.ConnectionType = 1; // подключение через TCP socket
            Drv.ProtocolType = 0; // Стандартный протокол Drv.IPAddress := '192.168.0.2'; // IP адрес ККТ
            Drv.UseIPAddress = true; // Используем свойство IPAddress
            Drv.IPAddress = "127.0.0.1";
            Drv.TCPPort = 7778;
//            Drv.Timeout = 500;
            Drv.Password = 30;

            Drv.ServerConnect();
            return Drv.ServerConnected; 
            */

        }

        public string getDriverVersion()
        {
            return Drv.DriverVersion;
        }

        public void AboutBox()
        {
            Drv.AboutBox();
        }

        public void SetPassword(int pwd)
        {
            Drv.Password = pwd;
        }

        // Гудок
        public void Beep()
        {
            Drv.Beep();
        }

        // Показать окно Параметров
        public void ShowProperties()
        {
            Drv.ShowProperties();
        }

        // X-отчет
        public bool XReport()
        {
            Drv.PrintReportWithoutCleaning();
            SetCode();
            return ResultCode == 0;
        }

        // Z-отчет
        public bool ZReport()
        {
            Drv.PrintReportWithCleaning();
            SetCode();
            return ResultCode == 0;
        }

        // ПечатьЖирнойСтроки
        public bool PrintWideString(string s)
        {
            Drv.StringForPrinting = s;

            Drv.PrintWideString();
            SetCode();
            return ResultCode == 0;
        }

        // ПечатьСтроки
        public bool PrintString(string s)
        {
            Drv.StringForPrinting = s;

            Drv.PrintString();
            SetCode();
            return ResultCode == 0;
        }

        // ПечатьКартинки
        public bool Draw(int first = 1, int last = 84)
        {
            Drv.FirstLineNumber = first;
            Drv.LastLineNumber = last;

            Drv.Draw();
            SetCode();
            return ResultCode == 0;
        }

        // ОткрытьДенежныйЯщик
        public bool OpenDrawer(int drawer = 0)
        {
            Drv.DrawerNumber = drawer;

            Drv.OpenDrawer();
            SetCode();
            return ResultCode == 0;
        }

        // ОткрытьСмену
        public bool OpenSession()
        {
            Drv.OpenSession();
            SetCode();
            return ResultCode == 0;
        }

        // Продажа - ФНОперация FNOperation
        public bool Sale(double quantity, decimal price, int department, string tovar)
        {
            Drv.CheckType = 1; // Приход
            Drv.Quantity = quantity;
            Drv.Price = price;

            Drv.Summ1Enabled = false;   // Указываем, что НЕ сами рассчитываем цену
            //Drv.Summ1 = sum;

            Drv.TaxValueEnabled = false; // Налог мы не рассчитываем
            Drv.Tax1 = 7; //7 - НДС 5%; 8 - НДС 7%; 9 - НДС 5/105
            Drv.Department = department;

            Drv.PaymentTypeSign = 4; // Признак способа расчета (Полный расчет) - Необходим для ФФД 1.05
            Drv.PaymentItemSign = 1; // Признак предмета расчета (Товар) - Необходим для ФФД 1.05 

            Drv.StringForPrinting = tovar; // Наименование товара

            Drv.FNOperation();
            SetCode();
            return ResultCode == 0;
        }

        // Возврат - ФНОперация FNOperation
        public bool ReturnSale(double quantity, decimal price, int department, string tovar)
        {
            Drv.CheckType = 2; // Возврат прихода
            Drv.Quantity = quantity;
            Drv.Price = price;

            Drv.Summ1Enabled = false;   // Указываем, что НЕ сами рассчитываем цену
            //Drv.Summ1 = sum;

            Drv.TaxValueEnabled = false; // Налог мы не рассчитываем
            Drv.Tax1 = 7; //7 - НДС 5%; 8 - НДС 7%; 9 - НДС 5/105
            Drv.Department = department;

            Drv.PaymentTypeSign = 4; // Признак способа расчета (Полный расчет) - Необходим для ФФД 1.05
            Drv.PaymentItemSign = 1; // Признак предмета расчета (Товар) - Необходим для ФФД 1.05 

            Drv.StringForPrinting = tovar; // Наименование товара

            Drv.FNOperation();
            SetCode();
            return ResultCode == 0;
        }

        // ФНЗакрытиеЧекаРасш
        public bool FNCloseCheckEx(decimal sum, bool cash)
        {
            Drv.Summ1 = (cash == true) ? sum : 0;  // Наличные
            Drv.Summ2 = (cash == false) ? sum : 0; // Безналичные
            Drv.Summ3 = 0; Drv.Summ4 = 0; Drv.Summ5 = 0; Drv.Summ6 = 0; Drv.Summ7 = 0;
            Drv.Summ8 = 0; Drv.Summ9 = 0; Drv.Summ10 = 0; Drv.Summ11 = 0; Drv.Summ12 = 0;
            Drv.Summ13 = 0; Drv.Summ14 = 0; Drv.Summ15 = 0; Drv.Summ16 = 0;

            Drv.RoundingSumm = 0; // Сумма округления

            Drv.TaxValue1 = 0; Drv.TaxValue2 = 0; Drv.TaxValue3 = 0; // Налоги мы не считаем
            Drv.TaxValue4 = 0; Drv.TaxValue5 = 0; Drv.TaxValue6 = 0;

            Drv.TaxType = 4;    // Упрощенная доход-расход 0000 0100 <- бит2 = 1
            Drv.StringForPrinting = "";

            Drv.FNCloseCheckEx();
            SetCode();
            return ResultCode == 0;
        }

        // АннулироватьЧек
        // Работает в режиме 8 (см. свойство ECRMode)
        public bool CancelCheck()
        {
            Drv.CancelCheck();
            SetCode();
            return ResultCode == 0;
        }

        // ОтрезатьЧек
        public bool CutCheck()
        {
            Drv.CutType = false;
            Drv.FeedAfterCut = false;
            Drv.FeedLineCount = 2;

            Drv.CutCheck();
            SetCode();
            return ResultCode == 0;
        }

        // ПродолжитьПечать
        public bool ContinuePrint()
        {
            Drv.ContinuePrint();
            SetCode();
            return ResultCode == 0;
        }

        // ПолучитьСостояниеККМ
        public bool GetECRStatus()
        {
            Drv.GetECRStatus();
            SetCode();

            if (ResultCode == 0)
            {
                // 0 Принтер в рабочем режиме
                // 1 Выдача данных
                // 2 Открытая смена, 24 часа не кончились
                // 3 Открытая смена, 24 часа кончились
                // 4 Закрытая смена
                // 5 Блокировка по неправильному паролю налогового инспектора
                // 6 Ожидание подтверждения ввода даты
                // 7 Разрешение изменения положения десятичной точки
                // 8 Открытый документ
                // 9 Режим разрешения технологического обнуления
                // 10 Тестовый прогон
                // 11 Печать полного фискального отчета
                // 12 Печать длинного отчета ЭКЛЗ
                // 13 Работа с фискальным подкладным документом
                // 14 Печать подкладного документа
                // 15 Фискальный подкладной документ сформирован

                int ECRMode = Drv.ECRMode;
                int OpenDocumentNumber = Drv.OpenDocumentNumber;

                ResultData[0] = ECRMode.ToString();
                ResultData[1] = OpenDocumentNumber.ToString();
            }

            return ResultCode == 0;
        }


        // ПолучитьКороткийЗапросСостоянияККМ         
        public bool GetShortECRStatus()
        {
            Drv.GetShortECRStatus();
            SetCode();

            if (ResultCode == 0)
            {
                ResultData[0] = Drv.ECRMode.ToString();
                ResultData[1] = Drv.ECRModeDescription.ToString();
                ResultData[3] = Drv.ECRAdvancedMode.ToString();
                ResultData[4] = Drv.ECRAdvancedModeDescription.ToString();
            }

            return ResultCode > -1;
//            return ResultCode == 0;
        }

        // ПодытогЧека
        public bool CheckSubTotal()
        {
            Drv.CheckSubTotal();
            SetCode();

            if (ResultCode == 0)
            {
                decimal Summ1 = Drv.Summ1;
                ResultData[0] = Summ1.ToString();
            }

            return ResultCode == 0;
        }

        // ПолучитьДенежныйРегистр
        public bool GetCashReg(int reg)
        {
            Drv.RegisterNumber = reg;

            Drv.GetCashReg();
            SetCode();

            if (ResultCode == 0)
            {
                decimal ContentsOfCashRegister = Drv.ContentsOfCashRegister;
                ResultData[0] = ContentsOfCashRegister.ToString();
            }

            return ResultCode == 0;
        }

        // ПолучитьСтруктуруТаблицы
        public bool GetTableStruct(int table)
        {
            Drv.TableNumber = table;

            Drv.GetTableStruct();
            SetCode();

            if (ResultCode == 0)
            {
                ResultData[0] = Drv.TableName;
                ResultData[1] = Drv.RowNumber.ToString();
                ResultData[2] = Drv.FieldNumber.ToString();
            }

            return ResultCode == 0;
        }

        // ПолучитьСтруктуруПоля
        public bool GetFieldStruct(int table, int field)
        {
            Drv.TableNumber = table;
            Drv.FieldNumber = field;

            Drv.GetFieldStruct();
            SetCode();

            if (ResultCode == 0)
            {
                ResultData[0] = Drv.FieldName;
                ResultData[1] = Drv.FieldSize.ToString();
                ResultData[2] = Drv.FieldNumber.ToString();
                ResultData[3] = Drv.MINValueOfField.ToString();
                ResultData[4] = Drv.MAXValueOfField.ToString();
            }

            return ResultCode == 0;
        }

        // получить тип поля СТРОКА (true) или ЧИСЛО (false)
        private bool GetFieldType(int table, int field)
        {
            Drv.TableNumber = table;
            Drv.FieldNumber = field;

            Drv.GetFieldStruct();

            return Drv.FieldType;
        }

        // ПрочитатьТаблицу
        public bool ReadTable(int table, int row, int field)
        {
            Drv.TableNumber = table;
            Drv.RowNumber = row;
            Drv.FieldNumber = field;

            // получаем тип поля field в таблице table
            bool field_type = this.GetFieldType(table, field);

            Drv.ReadTable();
            SetCode();

            if (ResultCode == 0)
            {
                // пишет строку результата в зависимости от field_type
                ResultData[0] = field_type
                              ? Drv.ValueOfFieldString              // строка (true)
                              : Drv.ValueOfFieldInteger.ToString(); // число (false)
            }

            return ResultCode == 0;
        }

        // ПрочитатьТаблицу
        public bool WriteTable(int table, int row, int field, string value)
        {
            Drv.TableNumber = table;
            Drv.RowNumber = row;
            Drv.FieldNumber = field;

            // получаем тип поля field в таблице table
            bool field_type = this.GetFieldType(table, field);

            if (field_type == true)
            {
                Drv.ValueOfFieldString = value;
            }
            else
            {
                Drv.ValueOfFieldInteger = int.Parse(value);
            }

            Drv.WriteTable();
            SetCode();
            return ResultCode == 0;
        }
    }
}
