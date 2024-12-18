using CommunityToolkit.Mvvm.ComponentModel;
using FBDApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace FBDApp.Models
{
    public enum ModuleType
    {
        ToolStatus,           // Stream 1
        ToolControl,         // Stream 2
        ExceptionHandling,   // Stream 5
        DataCollection,      // Stream 6
        ProcessProgram,      // Stream 7
        Error,              // Stream 9
        TerminalServices    // Stream 10
    }

    public enum ComparisonOperator
    {
        GreaterThan,
        Equal,
        LessThan
    }

    [Serializable]
    public partial class SeceModule : ObservableObject
    {
        private string _id;
        private static bool _isDeserializing;

        public static void BeginDeserialization()
        {
            _isDeserializing = true;
        }

        public static void EndDeserialization()
        {
            _isDeserializing = false;
        }

        [JsonPropertyName("id")]
        public string Id 
        { 
            get => _id;
            set 
            {
                _id = value;
                LogService.LogInfo($"Set ID to: {value}");
            }
        }

        public SeceModule()
        {
            if (!_isDeserializing)
            {
                _id = Guid.NewGuid().ToString();
                LogService.LogInfo($"Created new SeceModule with ID: {_id} (Default constructor)");
            }
        }

        public SeceModule(string name, ModuleType type)
        {
            _id = Guid.NewGuid().ToString();
            Name = name;
            ModuleType = type;
            Width = 250;
            Height = 200;
            LogService.LogInfo($"Created new SeceModule with ID: {_id} (Name: {name}, Type: {type})");
        }

        // 創建模組的副本
        public SeceModule Clone()
        {
            var clone = new SeceModule()
            {
                Name = this.Name,
                ModuleType = this.ModuleType,
                X = this.X,
                Y = this.Y,
                Width = this.Width,
                Height = this.Height,
                OpcNodeId = this.OpcNodeId,
                SelectedSecsMessage = this.SelectedSecsMessage,
                SetValue = this.SetValue
            };
            // 克隆時生成新的ID
            clone._id = Guid.NewGuid().ToString();
            LogService.LogInfo($"Cloned module: Original ID: {this.Id}, New ID: {clone.Id}");
            return clone;
        }

        [OnDeserialized]
        private void OnDeserialized(System.Runtime.Serialization.StreamingContext context)
        {
            LogService.LogInfo($"Deserialization completed for module with ID: {_id}");
            UpdateAvailableSecsMessages();
            if (string.IsNullOrEmpty(SelectedSecsMessage) && AvailableSecsMessages.Any())
            {
                SelectedSecsMessage = AvailableSecsMessages.First();
            }
        }

        [ObservableProperty]
        [JsonPropertyName("name")]
        private string _name = "New Module";

        [ObservableProperty]
        [JsonPropertyName("x")]
        private double _x;

        [ObservableProperty]
        [JsonPropertyName("y")]
        private double _y;

        [ObservableProperty]
        [JsonPropertyName("width")]
        private double _width = 250;

        [ObservableProperty]
        [JsonPropertyName("height")]
        private double _height = 200;

        [ObservableProperty]
        [JsonPropertyName("isSelected")]
        private bool _isSelected;

        [ObservableProperty]
        [JsonPropertyName("opcNodeId")]
        private string _opcNodeId = string.Empty;

        [ObservableProperty]
        [JsonPropertyName("selectedSecsMessage")]
        private string _selectedSecsMessage = string.Empty;

        [ObservableProperty]
        [JsonPropertyName("setValue")]
        private double _setValue;

        [ObservableProperty]
        [JsonPropertyName("selectedOperator")]
        private ComparisonOperator _selectedOperator = ComparisonOperator.Equal;

        [ObservableProperty]
        [JsonPropertyName("moduleType")]
        private ModuleType _moduleType;

        partial void OnModuleTypeChanged(ModuleType value)
        {
            UpdateAvailableSecsMessages();
            if (AvailableSecsMessages.Any())
            {
                SelectedSecsMessage = AvailableSecsMessages.First();
            }
        }

        [JsonIgnore]
        [XmlIgnore]
        public IEnumerable<ComparisonOperator> AvailableOperators => Enum.GetValues<ComparisonOperator>();

        private ObservableCollection<string> _availableSecsMessages;
        public ObservableCollection<string> AvailableSecsMessages
        {
            get
            {
                if (_availableSecsMessages == null)
                {
                    _availableSecsMessages = new ObservableCollection<string>();
                    UpdateAvailableSecsMessages();
                }
                return _availableSecsMessages;
            }
        }

        private void UpdateAvailableSecsMessages()
        {
            if (_availableSecsMessages == null)
            {
                _availableSecsMessages = new ObservableCollection<string>();
            }
            else
            {
                _availableSecsMessages.Clear();
            }

            var messages = ModuleType switch
            {
                ModuleType.ToolStatus => new[]
                {
                    "S1F3",  // Selected Equipment Status Request (SESR)
                    "S1F4",  // Selected Equipment Status Data (SESD)
                    "S1F13", // Establish Communications Request (ECR)
                    "S1F14"  // Establish Communications Request Acknowledge (ECRA)
                },
                ModuleType.ToolControl => new[]
                {
                    "S2F23", // Transfer Command Send
                    "S2F24", // Transfer Command Acknowledge
                    "S2F41", // Host Command Send (HCS)
                    "S2F42"  // Host Command Acknowledge (HCA)
                },
                ModuleType.ExceptionHandling => new[]
                {
                    "S5F1",  // Alarm Report Send (ARS)
                    "S5F2",  // Alarm Report Acknowledge (ARA)
                    "S5F3",  // Enable/Disable Alarm Send (EAS)
                    "S5F4"   // Enable/Disable Alarm Acknowledge (EAA)
                },
                ModuleType.DataCollection => new[]
                {
                    "S6F3",  // Discrete Variable Data Send (DVS)
                    "S6F4",  // Discrete Variable Data Acknowledge (DVA)
                    "S6F5",  // Multi-block Data Send Inquire (MDI)
                    "S6F6"   // Multi-block Data Send Grant (MDG)
                },
                ModuleType.ProcessProgram => new[]
                {
                    "S7F1",  // Process Program Load Inquire (PPI)
                    "S7F2",  // Process Program Load Grant (PPG)
                    "S7F3",  // Process Program Send (PPS)
                    "S7F4"   // Process Program Acknowledge (PPA)
                },
                ModuleType.Error => new[]
                {
                    "S9F1",  // Unrecognized Device ID
                    "S9F3",  // Unrecognized Stream Type
                    "S9F5",  // Unrecognized Function Type
                    "S9F7"   // Illegal Data
                },
                ModuleType.TerminalServices => new[]
                {
                    "S10F1", // Terminal Request (VTR)
                    "S10F2", // Terminal Acknowledge (VTA)
                    "S10F3", // Terminal Display (VTD)
                    "S10F4"  // Terminal Display Acknowledge (VTDA)
                },
                _ => Array.Empty<string>()
            };

            foreach (var message in messages)
            {
                _availableSecsMessages.Add(message);
            }

            // 只有在 SelectedSecsMessage 為空或不在新的可用消息列表中時才重置
            if (string.IsNullOrEmpty(SelectedSecsMessage) || !_availableSecsMessages.Contains(SelectedSecsMessage))
            {
                SelectedSecsMessage = _availableSecsMessages.FirstOrDefault() ?? string.Empty;
            }
        }
    }
}
