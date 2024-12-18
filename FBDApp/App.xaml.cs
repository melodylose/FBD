using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Threading;
using FBDApp.ViewModels;
using FBDApp.Services;

namespace FBDApp
{
    /// <summary>
    /// Main application class that handles dependency injection setup,
    /// global exception handling, and application lifecycle management.
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider serviceProvider;

        public App()
        {
            // Register global exception handlers
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                ServiceCollection services = new ServiceCollection();
                ConfigureServices(services);
                serviceProvider = services.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Application initialization failed");
                MessageBox.Show("Application initialization failed. Please check the log file for details.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        /// <summary>
        /// Configures the dependency injection container with required services.
        /// </summary>
        /// <param name="services">The service collection to configure</param>
        private void ConfigureServices(ServiceCollection services)
        {
            // Register your services here
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        }

        /// <summary>
        /// Handles application startup by creating and showing the main window.
        /// </summary>
        /// <param name="e">Startup event arguments</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                var mainWindow = serviceProvider.GetService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Application startup failed");
                MessageBox.Show("Application startup failed. Please check the log file for details.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        /// <summary>
        /// Handles unhandled exceptions in the UI thread.
        /// Logs the error and shows a user-friendly message.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Exception event arguments</param>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogService.LogError(e.Exception, "Unhandled UI exception");
            MessageBox.Show($"An unhandled error occurred: {e.Exception.Message}\nPlease check the log file for details.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        /// <summary>
        /// Handles unhandled exceptions in non-UI threads.
        /// Logs the error and shows a user-friendly message.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Exception event arguments</param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogService.LogError(ex, "Unhandled application exception");
                MessageBox.Show($"A critical error occurred: {ex.Message}\nPlease check the log file for details.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
