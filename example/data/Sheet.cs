using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Runtime.InteropServices.ComTypes;


namespace Data
{
    public class Sheet : IEquatable<Sheet>
    {
        public enum Categories
        {
            Unknown,
            Struct,
            Array,
            Value
        };

        public event EventHandler ValueChanged;
        public delegate void RegistrationDelType(Sheet sheet);

        public Int64 Id { get => id_; private set => id_ = value; }

        public Int64 MaxId
        {
            get
            {
                if (parent_ != null) return parent_.MaxId;
                else return nextId_;
            }
        }

        public string Name { get => name_; }
        public string FullName { get => fullName_; }
        public Categories Category { get => category_; }
        public System.Type Type { get => type_; }

        public bool IsVisible { get => isVisible_; }
        public bool IsVariable { get => (category_ == Categories.Value); }
        public Int64 Size { get => size_; }
        public Int64 Offset { get => offset_; }
        public byte[] GData { get => CopyGBuf_(); }
        public byte[] LData { get => CopyLBuf_(); }

        public bool this[int key]
        {
            get
            {
                Int64 v1 = IValue; Int64 v2 = 1;
                return (v1 & (v2 << key)) != 0;
            }
            set
            {
                Int64 v1 = IValue;
                Int64 v2 = 1;
                v2 = (v2 << key);
                IValue = (value ? (v1 | v2) : (v1 & ~v2));
            }
        }

        public Int64 this[int from, int to]
        {
            get
            {
                UInt64 v = (UInt64)IValue;
                v = (v << (63 - to));
                v = (v >> (63 - (to - from)));
                return (Int64)v;
            }
            set
            {
                UInt64 v = (UInt64)value;
                v = (v << (63 - (to - from)));
                v = (v >> (63 - to));
                UInt64 v2 = (UInt64)IValue & ~GetMask_(from, to);
                v |= v2;
                IValue = (Int64)v;
            }
        }

        public Int64 IValue { get => getIValueDel_(); set => setIValueDel_(value); }
        public float FValue { get => getFValueDel_(); set => setFValueDel_(value); }
        public double DValue { get => getDValueDel_(); set => setDValueDel_(value); }

        public Sheet() { gDataLock_ = new object(); }
        private Sheet(Sheet parent) { parent_ = parent; }

        public bool Equals(Sheet other)
        {
            if (other == null) return false;
            return (Id == other.Id);
        }

        public Int64 Add(Int64 idParent, Dictionary<string, string> map)
        {
            if (Id == -1)
            {
                return Set_(map);
            }
            else
            {
                if (idParent == Id)
                    children_.Add(new Sheet(this));
                return children_.Last().Add(idParent, map);
            }
        }

        public string Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(FullName + '\n');
            foreach (Sheet s in children_)
                sb.Append(s.Print());
            return sb.ToString();
        }

        public void UpdateValue(byte[] data, Int64 sourceIndex)
        {
            if (parent_ == null)
            {
                DToGBuf_(data, sourceIndex);
                SendMessages_(data, sourceIndex);
            }
        }

        public RegistrationDelType Registration;

        private Int64 Set_(IDictionary<string, string> map)
        {
            Id = NextId_();
            name_ = map["name"];
            fullName_ = FName_();
            isVisible_ = Visible_(map);
            SetDataType_(map);
            size_ = Int64.Parse(map["size"]);
            offset_ = Int64.Parse(map["offset"]);

            if (parent_ == null)
            {
                gData_ = new byte[size_];
                gDataNew_ = new byte[size_];
                gDataOld_ = new byte[size_];
                variables_ = new List<Sheet>();
            }
            else gData_ = GData_();
            if (IsVariable) lData_ = new byte[size_];

            gDataLock_ = GDataLock_();

            Registration_(this);
            return Id;
        }

        private void SetDataType_(IDictionary<string, string> map)
        {
            category_ = Categories.Unknown;
            type_ = typeof(object);
            setIValueDel_ = null;
            getIValueDel_ = null;
            setFValueDel_ = null;
            getFValueDel_ = null;
            setDValueDel_ = null;
            getDValueDel_ = null;

            if (map["type"] == "STRUCT")
            {
                category_ = Categories.Struct;
            }
            else if (map["type"] == "ARRAY")
            {
                category_ = Categories.Array;
            }
            else
            {
                category_ = Categories.Value;
                if (map["subtype"] == "uint16_t")
                {
                    type_ = typeof(UInt16); getIValueDel_ = GetIValueForUInt16_; setIValueDel_ = SetIValueForUInt16_;
                }
                else if (map["subtype"] == "uint8_t")
                {
                    type_ = typeof(SByte); getIValueDel_ = GetIValueForSByte_; setIValueDel_ = SetIValueForSByte_;
                }
                else if (map["subtype"] == "int8_t")
                {
                    type_ = typeof(Byte); getIValueDel_ = GetIValueForByte_; setIValueDel_ = SetIValueForByte_;
                }
                else if (map["subtype"] == "int16_t")
                {
                    type_ = typeof(Int16); getIValueDel_ = GetIValueForInt16_; setIValueDel_ = SetIValueForInt16_;
                }
                else if (map["subtype"] == "int32_t")
                {
                    type_ = typeof(Int32); getIValueDel_ = GetIValueForInt32_; setIValueDel_ = SetIValueForInt32_;
                }
                else if (map["subtype"] == "uint32_t")
                {
                    type_ = typeof(UInt32); getIValueDel_ = GetIValueForUInt32_; setIValueDel_ = SetIValueForUInt32_;
                }
                else if (map["subtype"] == "float")
                {
                    type_ = typeof(float); getFValueDel_ = GetFValueForFloat_; setFValueDel_ = SetFValueForFloat_;
                }
                else if (map["subtype"] == "double")
                {
                    type_ = typeof(double); getDValueDel_ = GetDValueForDouble_; setDValueDel_ = SetDValueForDouble_;
                }
                else if (map["subtype"] == "uint64_t")
                {
                    type_ = typeof(UInt64); getIValueDel_ = GetIValueForUInt64_; setIValueDel_ = SetIValueForUInt64_;
                }
                else if (map["subtype"] == "int64_t")
                {
                    type_ = typeof(Int64); getIValueDel_ = GetIValueForInt64_; setIValueDel_ = SetIValueForInt64_;
                }
            }
        }

        private void Registration_(Sheet sheet)
        {
            if (parent_ != null)
            {
                parent_.Registration_(sheet);
            }
            else
            {
                if (sheet.IsVariable) variables_.Add(sheet);
                if (Registration != null) Registration(sheet);
            }
        }

        private string FName_()
        {
            StringBuilder sb = new StringBuilder();
            if (parent_ != null)
            {
                sb.Append(parent_.FName_());
                sb.Append('.');
            }
            sb.Append(name_);
            return sb.ToString();
        }

        private bool Visible_(IDictionary<string, string> map)
        {
            return (map["visible"].ToLower() == "true" ? true : false) && Visible_();
        }

        private bool Visible_()
        {
            bool result = isVisible_;
            if (parent_ != null)
                result = result && parent_.Visible_();
            return result;
        }

        private T GetValue_<T>()
        {
            if (IsVariable)
            {
                GToLBuf_();
                return BufToValue_<T>();
            }
            else
            {
                return default(T);
            }
        }

        private void SetValue_<T>(T value)
        {
            if (IsVariable)
            {
                ValueToBuf_(value);
                LToGBuf_();
            }
        }

        private T BufToValue_<T>()
        {
            lock (lDataLock_)
            {
                GCHandle handle = GCHandle.Alloc(lData_, GCHandleType.Pinned);
                T result = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                handle.Free();
                return result;
            }
        }


        private void ValueToBuf_<T>(T value)
        {
            lock (lDataLock_)
            {
                GCHandle handle = GCHandle.Alloc(lData_, GCHandleType.Pinned);
                Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
                handle.Free();
            }
        }

        private Int64 GetIValueForSByte_() { return GetValue_<SByte>(); }
        private void SetIValueForSByte_(Int64 value) { SetValue_((SByte)value); }
        private Int64 GetIValueForByte_() { return GetValue_<Byte>(); }
        private void SetIValueForByte_(Int64 value) { SetValue_((Byte)value); }

        private Int64 GetIValueForInt16_() { return GetValue_<Int16>(); }
        private void SetIValueForInt16_(Int64 value) { SetValue_((Int16)value); }
        private Int64 GetIValueForUInt16_() { return GetValue_<UInt16>(); }
        private void SetIValueForUInt16_(Int64 value) { SetValue_((UInt16)value); }

        private Int64 GetIValueForInt32_() { return (Int64)GetValue_<Int32>(); }
        private void SetIValueForInt32_(Int64 value) { SetValue_((Int32)value); }
        private Int64 GetIValueForUInt32_() { return GetValue_<UInt32>(); }
        private void SetIValueForUInt32_(Int64 value) { SetValue_((UInt32)value); }

        private Int64 GetIValueForInt64_() { return GetValue_<Int64>(); }
        private void SetIValueForInt64_(Int64 value) { SetValue_((Int64)value); }
        private Int64 GetIValueForUInt64_() { return (Int64)GetValue_<UInt64>(); }
        private void SetIValueForUInt64_(Int64 value) { SetValue_((UInt64)value); }

        private float GetFValueForFloat_() { return GetValue_<float>(); }
        private void SetFValueForFloat_(float value) { SetValue_(value); }
        private double GetDValueForDouble_() { return GetValue_<double>(); }
        private void SetDValueForDouble_(double value) { SetValue_(value); }

        private void DToGBuf_(byte[] data, Int64 sourceIndex)
        {
            lock (gDataLock_)
            {
                Array.Copy(gData_, 0, gDataOld_, 0, gData_.Length);
                Array.Copy(data, sourceIndex, gData_, 0, gData_.Length);
            }
        }

        private void SendMessages_(byte[] data, Int64 sourceIndex)
        {
            if (variables_.Any())
            {
                Array.Copy(data, sourceIndex, gDataNew_, 0, gDataNew_.Length);

                int curInx = 0;
                Sheet sheet = variables_[curInx];
                Int64 nextEl = sheet.Offset + sheet.Size;

                for (Int64 i = 0; i < gDataNew_.Length;)
                {
                    if (nextEl <= i)
                    {
                        ++curInx;
                        sheet = variables_[curInx];
                        nextEl = sheet.Offset + sheet.Size;
                    }

                    if (gDataOld_[i] != gDataNew_[i])
                    {
                        i = nextEl;
                        sheet.SendEvent_();
                    }
                    else
                    {
                        ++i;
                    }
                }
            }
        }

        private void SendEvent_()
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GToLBuf_()
        {
            lock (gDataLock_) Array.Copy(gData_, offset_, lData_, 0, size_);
        }

        private void LToGBuf_()
        {
            lock (gDataLock_) Array.Copy(lData_, 0, gData_, offset_, size_);
        }

        private byte[] CopyGBuf_()
        {
            byte[] data = new byte[gData_.Length];
            lock (gDataLock_) Array.Copy(gData_, 0, data, 0, gData_.Length);
            return data;
        }

        private byte[] CopyLBuf_()
        {
            byte[] data = new byte[lData_.Length];
            lock (lDataLock_) Array.Copy(lData_, 0, data, 0, lData_.Length);
            return data;
        }

        private Int64 NextId_()
        {
            if (parent_ != null)
            {
                return parent_.NextId_();
            }
            else
            {
                ++nextId_;
                return nextId_;
            }
        }

        public byte[] GData_()
        {
            if (parent_ != null) return parent_.GData_();
            else return gData_;
        }

        private object GDataLock_()
        {
            if (parent_ != null) return parent_.GDataLock_();
            else return gDataLock_;
        }

        private static string ConvertToBinary_(UInt64 value)
        {
            string result = "0";
            if (value != 0)
            {
                StringBuilder sb = new StringBuilder();
                while (value != 0)
                {
                    sb.Insert(0, ((value & 1) == 1) ? '1' : '0');
                    value >>= 1;
                }
                result = sb.ToString();
            }
            return result.PadLeft(64, '0');
        }

        private static string ConvertToBinary_(Int64 value)
        {
            return Convert.ToString(value, 2).PadLeft(64, '0');
        }

        private static UInt64 GetMask_(int from, int to)
        {
            UInt64 mask = ((1UL << (to + 1)) - 1UL) ^ ((1UL << from) - 1UL);
            if (to == 63) mask = ~mask;
            return mask;
        }

        private delegate void SetIValueDelType_(Int64 value);
        private delegate Int64 GetIValueDelType_();

        private delegate void SetFValueDelType_(float value);
        private delegate float GetFValueDelType_();

        private delegate void SetDValueDelType_(double value);
        private delegate double GetDValueDelType_();

        private Int64 id_                                   = -1;
        private Int64 nextId_                               = -1;
        private string name_                                = string.Empty;
        private string fullName_                            = string.Empty;

        private Categories category_                        = Categories.Unknown;
        private System.Type type_                           = null;
        private bool isVisible_                             = true;
        private Int64 size_                                 = 0;
        private Int64 offset_                               = 0;
        private Sheet parent_                               = null;
        private byte[] gData_                               = null;
        private byte[] gDataNew_                            = null;
        private byte[] gDataOld_                            = null;
        private byte[] lData_                               = null;
        private List<Sheet> children_                       = new List<Sheet>();
        private List<Sheet> variables_                      = null;
        private object gDataLock_                           = null;
        private object lDataLock_                           = new object();

        private SetIValueDelType_ setIValueDel_             = null;
        private GetIValueDelType_ getIValueDel_             = null;
        private SetFValueDelType_ setFValueDel_             = null;
        private GetFValueDelType_ getFValueDel_             = null;
        private SetDValueDelType_ setDValueDel_             = null;
        private GetDValueDelType_ getDValueDel_             = null;
    }
}