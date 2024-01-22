using System;
using System.Collections;
using System.Collections.Generic;


namespace Data
{
    /// <summary>
    /// Класс <c>DataRegister</c> - это модель адресного
    /// пространства полученный по Modbus данных.
    /// </summary>
    public class DataRegister : IEnumerable
    {
        public static Int64 DataSize(string fileName)
        {
            return JsonParser.DataSize(fileName);
        }

        public static Int64 ExpDataSize()
        {
            return (int)EDN_.Size;
        }

        public static Int64 FullDataSize(string fileName)
        {
            return DataSize(fileName) + ExpDataSize();
        }

        private enum EDN_
        {
            Number,
            Address,
            Line,
            Size
        };

        /// <summary>
        /// Номер конвейера.
        /// </summary>
        public byte Number
        {
            get => expData_[(int)EDN_.Number];
            set => expData_[(int)EDN_.Number] = value;
        }

        /// <summary>
        /// Modbus-адрес.
        /// </summary>
        public byte Address
        {
            get => expData_[(int)EDN_.Address];
            set => expData_[(int)EDN_.Address] = value;
        }

        /// <summary>
        /// номер линии связи.
        /// </summary>
        public byte Line
        {
            get => expData_[(int)EDN_.Line];
            set => expData_[(int)EDN_.Line] = value;
        }

        /// <summary>
        /// Размер данных.
        /// </summary>
        public Int64 Size { get => root_.Size; }

        /// <summary>
        /// Данные.
        /// </summary>
        public byte[] Data
        {
            get => root_.GData;
            set => root_.UpdateValue(value, 0);
        }

        /// <summary>
        /// Размер расширенных данных.
        /// </summary>
        public Int64 ExpSize { get => expData_.Length; }

        /// <summary>
        /// Расширенные данные.
        /// </summary>
        public byte[] ExpData
        {
            get
            {
                byte[] result = new byte[expData_.Length];
                Array.Copy(expData_, 0, result, 0, expData_.Length);
                return result;
            }
            set
            {

            }
        }

        /// <summary>
        /// Полный размер данных.
        /// </summary>
        public Int64 FullSize { get => root_.Size + expData_.Length; }

        /// <summary>
        /// Полные данные.
        /// </summary>
        public byte[] FullData
        {
            get
            {
                byte[] result = new byte[FullSize];
                Array.Copy(expData_, 0, result, 0, expData_.Length);

                byte[] data = Data;
                Array.Copy(data, 0, result, expData_.Length, data.Length);
                return result;
            }
        }


        /// <summary>
        /// Конструктор без параметров.
        /// </summary>
        public DataRegister(string fileName)
        {
            root_ = new Sheet();
            names_ = new List<string>();
            idSheet_ = new Dictionary<Int64, Sheet>();
            nameSheet_ = new Dictionary<string, Sheet>();
            root_.Registration = Registration_;
            Sheeter sheeter = new Sheeter(root_);
            jsonParser_ = new JsonParser(fileName);
            jsonParser_.TreeParse(sheeter);
            expData_ = new byte[(int)EDN_.Size];
        }

        /// <summary>
        /// Метод устанавливает внутренние данные из входного
        /// байтового массива.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator_();
        }

        /// <summary>
        /// Метод устанавливает внутренние данные из входного
        /// байтового массива.
        /// </summary>
        /// <param name="data">байтовый массив данных.</param>
        /// <param name="sourceIndex">начало копирования.</param>
        public void SetData(byte[] data, Int64 sourceIndex = 0)
        {
            root_.UpdateValue(data, sourceIndex);
        }

        /// <summary>
        /// Метод устанавливает внутренние данные из входного
        /// байтового массива.
        /// </summary>
        /// <param name="data">байтовый массив данных.</param>
        /// <param name="sourceIndex">начало копирования.</param>
        public void SetData(ushort[] data, Int64 sourceIndex = 0)
        {
            byte[] data1 = new byte[data.Length * 2];
            Buffer.BlockCopy(data, 0, data1, 0, data.Length * 2);
            root_.UpdateValue(data1, sourceIndex * 2);
        }

        /// <summary>
        /// Метод устанавливает внутренние данные из входного
        /// байтового массива.
        /// </summary>
        /// <param name="data">байтовый массив данных.</param>
        /// <param name="sourceIndex">начало копирования.</param>
        public void SetFullData(byte[] data, Int64 sourceIndex = 0)
        {
            Array.Copy(data, sourceIndex, expData_, 0, expData_.Length);
            root_.UpdateValue(data, sourceIndex + ExpSize);
        }

        /// <summary>
        /// Метод устанавливает внутренние данные из входного
        /// байтового массива.
        /// </summary>
        /// <param name="data">байтовый массив данных.</param>
        /// <param name="sourceIndex">начало копирования.</param>
        public void SetFullData(ushort[] data, Int64 sourceIndex = 0)
        {
            byte[] data1 = new byte[data.Length * 2];
            Buffer.BlockCopy(data, 0, data1, 0, data.Length * 2);
            SetFullData(data1, sourceIndex * 2);
        }

        public Int64 MaxId { get => root_.MaxId; }
        public IList<string> Names { get => names_; }

        /// <summary>
        /// Индексатор по строке.
        /// </summary>
        /// <param name="key">имя данного.</param>
        public Sheet this[string key]
        {
            get
            {
                Sheet result = null;
                nameSheet_.TryGetValue(key, out result);
                return result;
            }
        }

        /// <summary>
        /// Индексатор по Int64.
        /// </summary>
        /// <param name="key">id данного.</param>
        public Sheet this[Int64 key]
        {
            get
            {
                Sheet result = null;
                idSheet_.TryGetValue(key, out result);
                return result;
            }
        }

        private void Registration_(Sheet sheet)
        {
            names_.Add(sheet.FullName);
            idSheet_.Add(sheet.Id, sheet);
            nameSheet_.Add(sheet.FullName, sheet);
        }

        private DataRegisterEnum GetEnumerator_()
        {
            return new DataRegisterEnum(this);
        }

        private Sheet root_;
        private List<string> names_;
        private Dictionary<Int64, Sheet> idSheet_;
        private Dictionary<string, Sheet> nameSheet_;
        private JsonParser jsonParser_;
        private byte[] expData_;    // расширенные данные
    }

    public class DataRegisterEnum : IEnumerator<Sheet>
    {
        public DataRegisterEnum(DataRegister dr)
        {
            dr_ = dr;
        }

        void IDisposable.Dispose() {}

        public bool MoveNext()
        {
            ++pos_;
            return (pos_ < dr_.Names.Count);
        }

        public void Reset()
        {
            pos_ = -1;
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public Sheet Current
        {
            get
            {
                try
                {
                    return dr_[pos_];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }
        
        private DataRegister dr_;
        private Int64 pos_ = -1;
    }
}