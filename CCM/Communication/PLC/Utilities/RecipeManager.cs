using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using CCM.Communication.Interfaces;

namespace CCM.Communication.PLC.Utilities
{
    /// <summary>
    /// 레시피 항목 정의
    /// </summary>
    [Serializable]
    public class RecipeItem
    {
        /// <summary>
        /// 항목 이름
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 디바이스 타입 (D, M, W 등)
        /// </summary>
        public string Device { get; set; }

        /// <summary>
        /// 주소
        /// </summary>
        public int Address { get; set; }

        /// <summary>
        /// 데이터 타입
        /// </summary>
        public RecipeDataType DataType { get; set; } = RecipeDataType.Word;

        /// <summary>
        /// 값
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 설명
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// 레시피 데이터 타입
    /// </summary>
    public enum RecipeDataType
    {
        Bit,
        Word,
        DWord,
        Real
    }

    /// <summary>
    /// 레시피 정의
    /// </summary>
    [Serializable]
    public class Recipe
    {
        /// <summary>
        /// 레시피 ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 레시피 이름
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 제품 코드
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// 버전
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 생성일
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 수정일
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 설명
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 레시피 항목 목록
        /// </summary>
        public List<RecipeItem> Items { get; set; } = new List<RecipeItem>();
    }

    /// <summary>
    /// 레시피 전송 결과
    /// </summary>
    public class RecipeTransferResult
    {
        public bool IsSuccess { get; set; }
        public int TotalItems { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public List<string> FailedItems { get; set; } = new List<string>();
        public string ErrorMessage { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }

    /// <summary>
    /// 레시피 전송 진행 이벤트 인자
    /// </summary>
    public class RecipeProgressEventArgs : EventArgs
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public string ItemName { get; set; }
        public bool IsSuccess { get; set; }
        public double ProgressPercent => Total > 0 ? (double)Current / Total * 100 : 0;
    }

    /// <summary>
    /// PLC 레시피 관리 클래스
    /// - 레시피 파일 저장/로드 (XML)
    /// - PLC로 레시피 다운로드
    /// - PLC에서 레시피 업로드
    /// - 제품별 레시피 관리
    /// </summary>
    public class RecipeManager
    {
        #region Fields

        private readonly IPlcCommunication _plc;

        #endregion

        #region Properties

        /// <summary>
        /// 항목 간 딜레이 (밀리초), 기본값 10ms
        /// </summary>
        public int ItemDelay { get; set; } = 10;

        #endregion

        #region Events

        /// <summary>
        /// 전송 진행 이벤트
        /// </summary>
        public event EventHandler<RecipeProgressEventArgs> Progress;

        #endregion

        #region Constructor

        /// <summary>
        /// RecipeManager 생성자
        /// </summary>
        /// <param name="plc">PLC 통신 인터페이스</param>
        public RecipeManager(IPlcCommunication plc)
        {
            _plc = plc ?? throw new ArgumentNullException(nameof(plc));
        }

        #endregion

        #region Download (PC → PLC)

        /// <summary>
        /// 레시피를 PLC로 다운로드
        /// </summary>
        /// <param name="recipe">레시피</param>
        /// <returns>전송 결과</returns>
        public RecipeTransferResult Download(Recipe recipe)
        {
            var result = new RecipeTransferResult
            {
                TotalItems = recipe.Items.Count
            };

            var startTime = DateTime.Now;

            if (!_plc.IsConnected)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "PLC가 연결되어 있지 않습니다.";
                return result;
            }

            for (int i = 0; i < recipe.Items.Count; i++)
            {
                var item = recipe.Items[i];
                bool success = WriteItem(item);

                if (success)
                {
                    result.SuccessCount++;
                }
                else
                {
                    result.FailCount++;
                    result.FailedItems.Add(item.Name);
                }

                Progress?.Invoke(this, new RecipeProgressEventArgs
                {
                    Current = i + 1,
                    Total = recipe.Items.Count,
                    ItemName = item.Name,
                    IsSuccess = success
                });

                if (ItemDelay > 0)
                    System.Threading.Thread.Sleep(ItemDelay);
            }

            result.ElapsedTime = DateTime.Now - startTime;
            result.IsSuccess = result.FailCount == 0;

            return result;
        }

        /// <summary>
        /// 레시피 파일을 PLC로 다운로드
        /// </summary>
        /// <param name="filePath">레시피 파일 경로</param>
        /// <returns>전송 결과</returns>
        public RecipeTransferResult DownloadFromFile(string filePath)
        {
            var recipe = LoadFromFile(filePath);
            return Download(recipe);
        }

        #endregion

        #region Upload (PLC → PC)

        /// <summary>
        /// PLC에서 레시피 업로드
        /// </summary>
        /// <param name="template">레시피 템플릿 (항목 정의)</param>
        /// <returns>값이 채워진 레시피</returns>
        public Recipe Upload(Recipe template)
        {
            var recipe = new Recipe
            {
                Id = template.Id,
                Name = template.Name,
                ProductCode = template.ProductCode,
                Version = template.Version,
                Description = template.Description,
                CreatedDate = template.CreatedDate,
                ModifiedDate = DateTime.Now
            };

            if (!_plc.IsConnected)
                throw new InvalidOperationException("PLC가 연결되어 있지 않습니다.");

            for (int i = 0; i < template.Items.Count; i++)
            {
                var templateItem = template.Items[i];
                var newItem = new RecipeItem
                {
                    Name = templateItem.Name,
                    Device = templateItem.Device,
                    Address = templateItem.Address,
                    DataType = templateItem.DataType,
                    Description = templateItem.Description
                };

                newItem.Value = ReadItemValue(templateItem);
                recipe.Items.Add(newItem);

                Progress?.Invoke(this, new RecipeProgressEventArgs
                {
                    Current = i + 1,
                    Total = template.Items.Count,
                    ItemName = templateItem.Name,
                    IsSuccess = true
                });

                if (ItemDelay > 0)
                    System.Threading.Thread.Sleep(ItemDelay);
            }

            return recipe;
        }

        /// <summary>
        /// PLC에서 레시피 업로드 후 파일로 저장
        /// </summary>
        /// <param name="template">레시피 템플릿</param>
        /// <param name="filePath">저장할 파일 경로</param>
        public void UploadToFile(Recipe template, string filePath)
        {
            var recipe = Upload(template);
            SaveToFile(recipe, filePath);
        }

        #endregion

        #region File Operations

        /// <summary>
        /// 레시피를 XML 파일로 저장
        /// </summary>
        public void SaveToFile(Recipe recipe, string filePath)
        {
            recipe.ModifiedDate = DateTime.Now;

            var serializer = new XmlSerializer(typeof(Recipe));
            using (var writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, recipe);
            }
        }

        /// <summary>
        /// XML 파일에서 레시피 로드
        /// </summary>
        public Recipe LoadFromFile(string filePath)
        {
            var serializer = new XmlSerializer(typeof(Recipe));
            using (var reader = new StreamReader(filePath))
            {
                return (Recipe)serializer.Deserialize(reader);
            }
        }

        #endregion

        #region Recipe Creation Helpers

        /// <summary>
        /// 새 레시피 생성
        /// </summary>
        public Recipe CreateRecipe(string id, string name, string productCode = null)
        {
            return new Recipe
            {
                Id = id,
                Name = name,
                ProductCode = productCode ?? id,
                Version = "1.0"
            };
        }

        /// <summary>
        /// 레시피에 워드 항목 추가
        /// </summary>
        public void AddWordItem(Recipe recipe, string name, string device, int address, short value, string description = null)
        {
            recipe.Items.Add(new RecipeItem
            {
                Name = name,
                Device = device,
                Address = address,
                DataType = RecipeDataType.Word,
                Value = value.ToString(),
                Description = description
            });
        }

        /// <summary>
        /// 레시피에 DWord 항목 추가
        /// </summary>
        public void AddDWordItem(Recipe recipe, string name, string device, int address, int value, string description = null)
        {
            recipe.Items.Add(new RecipeItem
            {
                Name = name,
                Device = device,
                Address = address,
                DataType = RecipeDataType.DWord,
                Value = value.ToString(),
                Description = description
            });
        }

        /// <summary>
        /// 레시피에 Real 항목 추가
        /// </summary>
        public void AddRealItem(Recipe recipe, string name, string device, int address, float value, string description = null)
        {
            recipe.Items.Add(new RecipeItem
            {
                Name = name,
                Device = device,
                Address = address,
                DataType = RecipeDataType.Real,
                Value = value.ToString(),
                Description = description
            });
        }

        /// <summary>
        /// 레시피에 비트 항목 추가
        /// </summary>
        public void AddBitItem(Recipe recipe, string name, string device, int address, bool value, string description = null)
        {
            recipe.Items.Add(new RecipeItem
            {
                Name = name,
                Device = device,
                Address = address,
                DataType = RecipeDataType.Bit,
                Value = value ? "1" : "0",
                Description = description
            });
        }

        #endregion

        #region Private Methods

        private bool WriteItem(RecipeItem item)
        {
            try
            {
                PlcResult result;

                switch (item.DataType)
                {
                    case RecipeDataType.Bit:
                        bool bitValue = item.Value == "1" || item.Value.ToLower() == "true";
                        result = _plc.WriteBit(item.Device, item.Address, bitValue);
                        break;

                    case RecipeDataType.Word:
                        short wordValue = short.Parse(item.Value);
                        result = _plc.WriteWord(item.Device, item.Address, wordValue);
                        break;

                    case RecipeDataType.DWord:
                        int dwordValue = int.Parse(item.Value);
                        result = _plc.WriteDWord(item.Device, item.Address, dwordValue);
                        break;

                    case RecipeDataType.Real:
                        float realValue = float.Parse(item.Value);
                        result = _plc.WriteReal(item.Device, item.Address, realValue);
                        break;

                    default:
                        return false;
                }

                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        private string ReadItemValue(RecipeItem item)
        {
            try
            {
                switch (item.DataType)
                {
                    case RecipeDataType.Bit:
                        var bitResult = _plc.ReadBit(item.Device, item.Address);
                        return bitResult.IsSuccess ? (bitResult.Value ? "1" : "0") : "0";

                    case RecipeDataType.Word:
                        var wordResult = _plc.ReadWord(item.Device, item.Address);
                        return wordResult.IsSuccess ? wordResult.Value.ToString() : "0";

                    case RecipeDataType.DWord:
                        var dwordResult = _plc.ReadDWord(item.Device, item.Address);
                        return dwordResult.IsSuccess ? dwordResult.Value.ToString() : "0";

                    case RecipeDataType.Real:
                        var realResult = _plc.ReadReal(item.Device, item.Address);
                        return realResult.IsSuccess ? realResult.Value.ToString() : "0";

                    default:
                        return "0";
                }
            }
            catch
            {
                return "0";
            }
        }

        #endregion
    }
}
