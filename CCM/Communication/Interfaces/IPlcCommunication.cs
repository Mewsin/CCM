using System;

namespace CCM.Communication.Interfaces
{
    /// <summary>
    /// PLC 디바이스 타입
    /// </summary>
    public enum PlcDeviceType
    {
        /// <summary>비트 디바이스 (X, Y, M 등)</summary>
        Bit,
        /// <summary>워드 디바이스 (D, W 등)</summary>
        Word,
        /// <summary>더블워드 디바이스</summary>
        DWord,
        /// <summary>실수 디바이스</summary>
        Real
    }

    /// <summary>
    /// PLC 읽기/쓰기 결과
    /// </summary>
    public class PlcResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public int ErrorCode { get; set; }

        public static PlcResult Success() => new PlcResult { IsSuccess = true };
        public static PlcResult Fail(string message, int errorCode = -1) => new PlcResult 
        { 
            IsSuccess = false, 
            ErrorMessage = message, 
            ErrorCode = errorCode 
        };
    }

    /// <summary>
    /// PLC 읽기 결과 (제네릭)
    /// </summary>
    public class PlcResult<T> : PlcResult
    {
        public T Value { get; set; }

        public static PlcResult<T> Success(T value) => new PlcResult<T> { IsSuccess = true, Value = value };
        public new static PlcResult<T> Fail(string message, int errorCode = -1) => new PlcResult<T> 
        { 
            IsSuccess = false, 
            ErrorMessage = message, 
            ErrorCode = errorCode 
        };
    }

    /// <summary>
    /// PLC 통신 인터페이스
    /// </summary>
    public interface IPlcCommunication : ICommunication
    {
        /// <summary>
        /// PLC 모델명
        /// </summary>
        string PlcModel { get; }

        #region 비트 읽기/쓰기

        /// <summary>
        /// 비트 읽기 (단일)
        /// </summary>
        PlcResult<bool> ReadBit(string device, int address);

        /// <summary>
        /// 비트 읽기 (연속)
        /// </summary>
        PlcResult<bool[]> ReadBits(string device, int startAddress, int count);

        /// <summary>
        /// 비트 쓰기 (단일)
        /// </summary>
        PlcResult WriteBit(string device, int address, bool value);

        /// <summary>
        /// 비트 쓰기 (연속)
        /// </summary>
        PlcResult WriteBits(string device, int startAddress, bool[] values);

        #endregion

        #region 워드 읽기/쓰기

        /// <summary>
        /// 워드 읽기 (단일, 16비트)
        /// </summary>
        PlcResult<short> ReadWord(string device, int address);

        /// <summary>
        /// 워드 읽기 (연속, 16비트)
        /// </summary>
        PlcResult<short[]> ReadWords(string device, int startAddress, int count);

        /// <summary>
        /// 워드 쓰기 (단일, 16비트)
        /// </summary>
        PlcResult WriteWord(string device, int address, short value);

        /// <summary>
        /// 워드 쓰기 (연속, 16비트)
        /// </summary>
        PlcResult WriteWords(string device, int startAddress, short[] values);

        #endregion

        #region 더블워드 읽기/쓰기

        /// <summary>
        /// 더블워드 읽기 (단일, 32비트)
        /// </summary>
        PlcResult<int> ReadDWord(string device, int address);

        /// <summary>
        /// 더블워드 읽기 (연속, 32비트)
        /// </summary>
        PlcResult<int[]> ReadDWords(string device, int startAddress, int count);

        /// <summary>
        /// 더블워드 쓰기 (단일, 32비트)
        /// </summary>
        PlcResult WriteDWord(string device, int address, int value);

        /// <summary>
        /// 더블워드 쓰기 (연속, 32비트)
        /// </summary>
        PlcResult WriteDWords(string device, int startAddress, int[] values);

        #endregion

        #region 실수 읽기/쓰기

        /// <summary>
        /// 실수 읽기 (단일, 32비트 float)
        /// </summary>
        PlcResult<float> ReadReal(string device, int address);

        /// <summary>
        /// 실수 읽기 (연속, 32비트 float)
        /// </summary>
        PlcResult<float[]> ReadReals(string device, int startAddress, int count);

        /// <summary>
        /// 실수 쓰기 (단일, 32비트 float)
        /// </summary>
        PlcResult WriteReal(string device, int address, float value);

        /// <summary>
        /// 실수 쓰기 (연속, 32비트 float)
        /// </summary>
        PlcResult WriteReals(string device, int startAddress, float[] values);

        #endregion

        #region 문자열 읽기/쓰기

        /// <summary>
        /// 문자열 읽기
        /// </summary>
        PlcResult<string> ReadString(string device, int address, int length);

        /// <summary>
        /// 문자열 쓰기
        /// </summary>
        PlcResult WriteString(string device, int address, string value);

        #endregion
    }
}
