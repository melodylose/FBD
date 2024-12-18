using System.Collections.ObjectModel;
using System.Threading.Tasks;
using FBDApp.Models;

namespace FBDApp.Services
{
    public interface IFileService
    {
        Task SaveConfiguration(string filePath, ObservableCollection<SeceModule> modules, ObservableCollection<ConnectionInfo> connections);
        Task<(ObservableCollection<SeceModule> modules, ObservableCollection<ConnectionInfo> connections)> LoadConfiguration(string filePath);
        string? ShowOpenFileDialog();
        string? ShowSaveFileDialog();
    }
}
