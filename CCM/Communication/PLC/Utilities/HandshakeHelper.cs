using System;
using System.Threading;
using CCM.Communication.Interfaces;

namespace CCM.Communication.PLC.Utilities
{
    /// <summary>
    /// 핸드쉐이크 명령 정의
    /// </summary>
    public class HandshakeCommand
    {
        /// <summary>
        /// 명령 ID (식별용)
        /// </summary>
        public int CommandId { get; set; }

        /// <summary>
        /// 명령 이름
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// PC → PLC 트리거 비트 디바이스 (예: M100)
        /// </summary>
        public string TriggerDevice { get; set; }

        /// <summary>
        /// PC → PLC 트리거 비트 주소
        /// </summary>
        public int TriggerAddress { get; set; }

        /// <summary>
        /// PLC → PC 완료 비트 디바이스 (예: M200)
        /// </summary>
        public string CompleteDevice { get; set; }

        /// <summary>
        /// PLC → PC 완료 비트 주소
        /// </summary>
        public int CompleteAddress { get; set; }

        /// <summary>
        /// 데이터 영역 디바이스 (옵션, 예: D100)
        /// </summary>
        public string DataDevice { get; set; }

        /// <summary>
        /// 데이터 시작 주소
        /// </summary>
        public int DataAddress { get; set; }

        /// <summary>
        /// 데이터 워드 수
        /// </summary>
        public int DataLength { get; set; }

        /// <summary>
        /// 타임아웃 (밀리초), 기본값 10초
        /// </summary>
        public int Timeout { get; set; } = 10000;
    }

    /// <summary>
    /// 핸드쉐이크 결과
    /// </summary>
    public class HandshakeResult
    {
        /// <summary>
        /// 성공 여부
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 에러 메시지
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 응답 데이터 (있는 경우)
        /// </summary>
        public short[] ResponseData { get; set; }

        /// <summary>
        /// 소요 시간 (밀리초)
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// 타임아웃 발생 여부
        /// </summary>
        public bool IsTimeout { get; set; }

        public static HandshakeResult Success(long elapsed, short[] data = null)
        {
            return new HandshakeResult
            {
                IsSuccess = true,
                ElapsedMilliseconds = elapsed,
                ResponseData = data
            };
        }

        public static HandshakeResult Fail(string message, bool isTimeout = false)
        {
            return new HandshakeResult
            {
                IsSuccess = false,
                ErrorMessage = message,
                IsTimeout = isTimeout
            };
        }
    }

    /// <summary>
    /// 핸드쉐이크 진행 상태
    /// </summary>
    public enum HandshakeState
    {
        /// <summary>대기 중</summary>
        Idle,
        /// <summary>트리거 전송됨, 완료 대기 중</summary>
        WaitingComplete,
        /// <summary>완료됨</summary>
        Completed,
        /// <summary>타임아웃</summary>
        Timeout,
        /// <summary>에러</summary>
        Error
    }

    /// <summary>
    /// 핸드쉐이크 이벤트 인자
    /// </summary>
    public class HandshakeEventArgs : EventArgs
    {
        public HandshakeCommand Command { get; }
        public HandshakeState State { get; }
        public HandshakeResult Result { get; }

        public HandshakeEventArgs(HandshakeCommand command, HandshakeState state, HandshakeResult result = null)
        {
            Command = command;
            State = state;
            Result = result;
        }
    }

    /// <summary>
    /// PC ↔ PLC 핸드쉐이크 헬퍼 클래스
    /// - PC → PLC: 트리거 비트 On
    /// - PLC 처리 후: 완료 비트 On
    /// - PC: 완료 확인 후 트리거 비트 Off
    /// - PLC: 트리거 Off 확인 후 완료 비트 Off
    /// </summary>
    public class HandshakeHelper : IDisposable
    {
        #region Fields

        private readonly IPlcCommunication _plc;
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>
        /// 폴링 간격 (밀리초), 기본값 50ms
        /// </summary>
        public int PollingInterval { get; set; } = 50;

        /// <summary>
        /// 트리거 Off 후 안정화 대기 시간 (밀리초), 기본값 100ms
        /// </summary>
        public int StabilizeDelay { get; set; } = 100;

        /// <summary>
        /// 완료 비트 Off 대기 여부 (기본값: true)
        /// </summary>
        public bool WaitForCompleteOff { get; set; } = true;

        /// <summary>
        /// 완료 비트 Off 대기 타임아웃 (밀리초), 기본값 5초
        /// </summary>
        public int CompleteOffTimeout { get; set; } = 5000;

        #endregion

        #region Events

        /// <summary>
        /// 핸드쉐이크 상태 변경 시 발생
        /// </summary>
        public event EventHandler<HandshakeEventArgs> StateChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// HandshakeHelper 생성자
        /// </summary>
        /// <param name="plc">PLC 통신 인터페이스</param>
        public HandshakeHelper(IPlcCommunication plc)
        {
            _plc = plc ?? throw new ArgumentNullException(nameof(plc));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 핸드쉐이크 실행 (동기)
        /// </summary>
        /// <param name="command">핸드쉐이크 명령</param>
        /// <param name="requestData">요청 데이터 (옵션)</param>
        /// <returns>핸드쉐이크 결과</returns>
        public HandshakeResult Execute(HandshakeCommand command, short[] requestData = null)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (!_plc.IsConnected)
                return HandshakeResult.Fail("PLC가 연결되어 있지 않습니다.");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 1. 요청 데이터 쓰기 (있는 경우)
                if (requestData != null && requestData.Length > 0 && !string.IsNullOrEmpty(command.DataDevice))
                {
                    var writeResult = _plc.WriteWords(command.DataDevice, command.DataAddress, requestData);
                    if (!writeResult.IsSuccess)
                        return HandshakeResult.Fail($"데이터 쓰기 실패: {writeResult.ErrorMessage}");
                }

                // 2. 트리거 비트 On
                var triggerResult = _plc.WriteBit(command.TriggerDevice, command.TriggerAddress, true);
                if (!triggerResult.IsSuccess)
                    return HandshakeResult.Fail($"트리거 비트 On 실패: {triggerResult.ErrorMessage}");

                OnStateChanged(command, HandshakeState.WaitingComplete);

                // 3. 완료 비트 On 대기
                bool completed = WaitForBit(command.CompleteDevice, command.CompleteAddress, true, command.Timeout);
                if (!completed)
                {
                    // 타임아웃 시 트리거 비트 Off
                    _plc.WriteBit(command.TriggerDevice, command.TriggerAddress, false);
                    OnStateChanged(command, HandshakeState.Timeout);
                    return HandshakeResult.Fail("완료 대기 타임아웃", true);
                }

                // 4. 응답 데이터 읽기 (있는 경우)
                short[] responseData = null;
                if (!string.IsNullOrEmpty(command.DataDevice) && command.DataLength > 0)
                {
                    var readResult = _plc.ReadWords(command.DataDevice, command.DataAddress, command.DataLength);
                    if (readResult.IsSuccess)
                        responseData = readResult.Value;
                }

                // 5. 트리거 비트 Off
                _plc.WriteBit(command.TriggerDevice, command.TriggerAddress, false);

                // 6. 완료 비트 Off 대기 (옵션)
                if (WaitForCompleteOff)
                {
                    WaitForBit(command.CompleteDevice, command.CompleteAddress, false, CompleteOffTimeout);
                }

                // 7. 안정화 대기
                if (StabilizeDelay > 0)
                    Thread.Sleep(StabilizeDelay);

                stopwatch.Stop();
                OnStateChanged(command, HandshakeState.Completed, 
                    HandshakeResult.Success(stopwatch.ElapsedMilliseconds, responseData));

                return HandshakeResult.Success(stopwatch.ElapsedMilliseconds, responseData);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                OnStateChanged(command, HandshakeState.Error);
                return HandshakeResult.Fail($"핸드쉐이크 실행 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 핸드쉐이크 실행 (간편 버전)
        /// </summary>
        public HandshakeResult Execute(
            string triggerDevice, int triggerAddress,
            string completeDevice, int completeAddress,
            int timeout = 10000)
        {
            var command = new HandshakeCommand
            {
                TriggerDevice = triggerDevice,
                TriggerAddress = triggerAddress,
                CompleteDevice = completeDevice,
                CompleteAddress = completeAddress,
                Timeout = timeout
            };

            return Execute(command);
        }

        /// <summary>
        /// 데이터 포함 핸드쉐이크 실행 (간편 버전)
        /// </summary>
        public HandshakeResult ExecuteWithData(
            string triggerDevice, int triggerAddress,
            string completeDevice, int completeAddress,
            string dataDevice, int dataAddress, int dataLength,
            short[] requestData = null,
            int timeout = 10000)
        {
            var command = new HandshakeCommand
            {
                TriggerDevice = triggerDevice,
                TriggerAddress = triggerAddress,
                CompleteDevice = completeDevice,
                CompleteAddress = completeAddress,
                DataDevice = dataDevice,
                DataAddress = dataAddress,
                DataLength = dataLength,
                Timeout = timeout
            };

            return Execute(command, requestData);
        }

        /// <summary>
        /// 트리거만 보내고 완료 대기하지 않음 (Fire and Forget)
        /// </summary>
        public PlcResult SendTrigger(string device, int address)
        {
            if (!_plc.IsConnected)
                return PlcResult.Fail("PLC가 연결되어 있지 않습니다.");

            return _plc.WriteBit(device, address, true);
        }

        /// <summary>
        /// 트리거 펄스 전송 (On → Off)
        /// </summary>
        /// <param name="device">디바이스</param>
        /// <param name="address">주소</param>
        /// <param name="pulseWidth">펄스 폭 (밀리초), 기본값 100ms</param>
        public PlcResult SendPulse(string device, int address, int pulseWidth = 100)
        {
            if (!_plc.IsConnected)
                return PlcResult.Fail("PLC가 연결되어 있지 않습니다.");

            var onResult = _plc.WriteBit(device, address, true);
            if (!onResult.IsSuccess)
                return onResult;

            Thread.Sleep(pulseWidth);

            return _plc.WriteBit(device, address, false);
        }

        /// <summary>
        /// 비트 상태 대기
        /// </summary>
        /// <param name="device">디바이스</param>
        /// <param name="address">주소</param>
        /// <param name="targetState">대기할 상태</param>
        /// <param name="timeout">타임아웃 (밀리초)</param>
        /// <returns>상태 도달 여부</returns>
        public bool WaitForBit(string device, int address, bool targetState, int timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                var result = _plc.ReadBit(device, address);
                if (result.IsSuccess && result.Value == targetState)
                    return true;

                Thread.Sleep(PollingInterval);
            }

            return false;
        }

        /// <summary>
        /// 워드 값 대기
        /// </summary>
        /// <param name="device">디바이스</param>
        /// <param name="address">주소</param>
        /// <param name="targetValue">대기할 값</param>
        /// <param name="timeout">타임아웃 (밀리초)</param>
        /// <returns>값 도달 여부</returns>
        public bool WaitForWord(string device, int address, short targetValue, int timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                var result = _plc.ReadWord(device, address);
                if (result.IsSuccess && result.Value == targetValue)
                    return true;

                Thread.Sleep(PollingInterval);
            }

            return false;
        }

        /// <summary>
        /// 워드 값 범위 대기
        /// </summary>
        /// <param name="device">디바이스</param>
        /// <param name="address">주소</param>
        /// <param name="minValue">최소값</param>
        /// <param name="maxValue">최대값</param>
        /// <param name="timeout">타임아웃 (밀리초)</param>
        /// <returns>범위 내 도달 여부</returns>
        public bool WaitForWordInRange(string device, int address, short minValue, short maxValue, int timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                var result = _plc.ReadWord(device, address);
                if (result.IsSuccess && result.Value >= minValue && result.Value <= maxValue)
                    return true;

                Thread.Sleep(PollingInterval);
            }

            return false;
        }

        /// <summary>
        /// 비트 상태 확인
        /// </summary>
        public bool CheckBit(string device, int address)
        {
            var result = _plc.ReadBit(device, address);
            return result.IsSuccess && result.Value;
        }

        /// <summary>
        /// 비트 강제 리셋 (트리거, 완료 비트 모두 Off)
        /// </summary>
        public PlcResult ResetBits(HandshakeCommand command)
        {
            var triggerResult = _plc.WriteBit(command.TriggerDevice, command.TriggerAddress, false);
            if (!triggerResult.IsSuccess)
                return triggerResult;

            // 완료 비트는 PLC에서 관리하므로 필요시에만 리셋
            // (일반적으로 PLC 측에서 트리거 Off 확인 후 완료 비트를 Off함)

            return PlcResult.Success();
        }

        #endregion

        #region Private Methods

        private void OnStateChanged(HandshakeCommand command, HandshakeState state, HandshakeResult result = null)
        {
            StateChanged?.Invoke(this, new HandshakeEventArgs(command, state, result));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // 관리되는 리소스 해제
            }

            _disposed = true;
        }

        ~HandshakeHelper()
        {
            Dispose(false);
        }

        #endregion
    }
}
