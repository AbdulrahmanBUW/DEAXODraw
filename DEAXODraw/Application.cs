using System;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DEAXODraw.Commands;

namespace DEAXODraw
{
    [Transaction(TransactionMode.Manual)]
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Create ribbon tab
                string tabName = "DEAXO Draw";
                application.CreateRibbonTab(tabName);

                // Create ribbon panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Modify");

                // Add AutoElevation button
                AddPushButton(panel, "AutoElevation", "Auto\nElevation",
                    typeof(AutoElevationCommand), "AutoElevation",
                    "Generates automated elevation views for the selected elements.");

                // Add AutoSection button
                AddPushButton(panel, "AutoSection", "Auto\nSection",
                    typeof(AutoSectionCommand), "AutoSection",
                    "Generates automated section views for the selected elements.");

                // Add separator
                panel.AddSeparator();

                // Add MakeParallel button
                AddPushButton(panel, "MakeParallel", "Make\nParallel",
                    typeof(MakeParallelCommand), "MakeParallel",
                    "Makes 2 elements parallel to each other. First element selected acts as reference.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to initialize DEAXO Draw: {ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void AddPushButton(RibbonPanel panel, string buttonName, string buttonText,
            Type commandType, string iconName, string tooltip)
        {
            PushButtonData buttonData = new PushButtonData(buttonName, buttonText,
                Assembly.GetExecutingAssembly().Location, commandType.FullName);

            PushButton button = panel.AddItem(buttonData) as PushButton;
            button.ToolTip = tooltip;

            // Set icons
            button.LargeImage = GetEmbeddedImage($"{iconName}_32.png");
            button.Image = GetEmbeddedImage($"{iconName}_16.png");
        }

        private BitmapImage GetEmbeddedImage(string imageName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"DEAXODraw.Resources.{imageName}";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = stream;
                        image.EndInit();
                        return image;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the application
                System.Diagnostics.Debug.WriteLine($"Failed to load image {imageName}: {ex.Message}");
            }

            return null;
        }
    }
}