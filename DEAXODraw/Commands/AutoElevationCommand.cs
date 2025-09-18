using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using DEAXODraw.Forms;
using DEAXODraw.Utilities;

namespace DEAXODraw.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class AutoElevationCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uidoc = uiApp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Step 1: Category Selection
                var categoryForm = new CategorySelectionForm();
                if (categoryForm.ShowDialog() != DialogResult.OK || !categoryForm.SelectedCategories.Any())
                {
                    TaskDialog.Show("Selection Required", "No categories were selected. Please try again.");
                    return Result.Cancelled;
                }

                // Step 2: Element Selection
                var selectionFilter = new DEAXOSelectionFilter(categoryForm.SelectedCategories);
                List<Element> selectedElements;

                try
                {
                    TaskDialog.Show("Element Selection",
                        "Select elements and press 'Finish' when done.\nPress 'Esc' to cancel.");

                    var references = uidoc.Selection.PickObjects(ObjectType.Element, selectionFilter,
                        "Select elements for elevation views");

                    selectedElements = references.Select(r => doc.GetElement(r)).ToList();
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                if (!selectedElements.Any())
                {
                    TaskDialog.Show("Selection Failed", "No elements were selected. Please try again.");
                    return Result.Failed;
                }

                // Step 3: View Template Selection
                var templateForm = new ViewTemplateSelectionForm(doc);
                View selectedViewTemplate = null;

                if (templateForm.ShowDialog() == DialogResult.OK)
                {
                    selectedViewTemplate = templateForm.SelectedViewTemplate;
                }

                // Step 4: Process Elements and Create Views
                return ProcessElements(doc, selectedElements, selectedViewTemplate);
            }
            catch (Exception ex)
            {
                message = $"Error in AutoElevation command: {ex.Message}";
                return Result.Failed;
            }
        }

        private Result ProcessElements(Document doc, List<Element> elements, View viewTemplate)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "DEAXO AutoElevation - Create Elevation Views"))
                {
                    trans.Start();

                    var results = new List<ElevationResult>();
                    int successCount = 0;
                    int failCount = 0;

                    foreach (Element element in elements)
                    {
                        try
                        {
                            var result = ProcessSingleElement(doc, element, viewTemplate);
                            if (result != null)
                            {
                                results.Add(result);
                                successCount++;
                            }
                            else
                            {
                                failCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing element {element.Id}: {ex.Message}");
                            failCount++;
                        }
                    }

                    trans.Commit();

                    // Show results
                    ShowResults(results, successCount, failCount);
                    return Result.Succeeded;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to create elevation views: {ex.Message}");
                return Result.Failed;
            }
        }

        private ElevationResult ProcessSingleElement(Document doc, Element element, View viewTemplate)
        {
            try
            {
                // Get element properties
                var elementProps = new ElementProperties(element, doc);
                if (!elementProps.IsValid)
                {
                    return null;
                }

                // Create section generator
                var generator = new SectionGenerator(doc, elementProps.Origin, elementProps.Vector,
                    elementProps.Width, elementProps.Height, elementProps.Offset,
                    elementProps.Depth, elementProps.DepthOffset);

                // Get element type name
                ElementType elementType = doc.GetElement(element.GetTypeId()) as ElementType;
                string typeName = elementType?.Name ?? "Unknown";
                string categoryName = element.Category?.Name ?? "Unknown";

                string viewNameBase = $"{typeName}_{element.Id}";

                // Create only elevation view
                ViewSection elevationView = generator.CreateElevationOnly(viewNameBase);
                if (elevationView == null)
                {
                    return null;
                }

                // Apply view template if selected
                if (viewTemplate != null)
                {
                    elevationView.ViewTemplateId = viewTemplate.Id;
                }

                // Create new sheet and place view
                ViewSheet newSheet = CreateSheetWithView(doc, elevationView, typeName, categoryName, element.Id);

                return new ElevationResult
                {
                    Element = element,
                    CategoryName = categoryName,
                    TypeName = typeName,
                    Sheet = newSheet,
                    ElevationView = elevationView
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing element: {ex.Message}");
                return null;
            }
        }

        private ViewSheet CreateSheetWithView(Document doc, ViewSection view, string typeName,
            string categoryName, ElementId elementId)
        {
            try
            {
                // Get default title block
                ElementId titleBlockId = doc.GetDefaultFamilyTypeId(new ElementId(BuiltInCategory.OST_TitleBlocks));
                if (titleBlockId == ElementId.InvalidElementId)
                {
                    // Find any title block if default is not available
                    var titleBlocks = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_TitleBlocks)
                        .WhereElementIsElementType()
                        .FirstOrDefault();

                    if (titleBlocks != null)
                        titleBlockId = titleBlocks.Id;
                }

                // Create new sheet
                ViewSheet newSheet = ViewSheet.Create(doc, titleBlockId);

                // Place view on sheet
                PlaceViewOnSheet(doc, view, newSheet);

                // Set sheet properties
                string sheetNumber = $"DEAXO_{typeName}_{elementId}";
                string sheetName = $"{categoryName} - Elevation (DEAXO GmbH)";

                // Ensure unique sheet number
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        newSheet.SheetNumber = sheetNumber;
                        newSheet.Name = sheetName;
                        break;
                    }
                    catch
                    {
                        sheetNumber += "*";
                    }
                }

                return newSheet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating sheet: {ex.Message}");
                return null;
            }
        }

        private void PlaceViewOnSheet(Document doc, ViewSection view, ViewSheet sheet)
        {
            try
            {
                XYZ position = new XYZ(-0.85, 0.65, 0);

                if (Viewport.CanAddViewToSheet(doc, sheet.Id, view.Id))
                {
                    Viewport.Create(doc, sheet.Id, view.Id, position);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error placing view on sheet: {ex.Message}");
            }
        }

        private void ShowResults(List<ElevationResult> results, int successCount, int failCount)
        {
            try
            {
                string message = $"AutoElevation Results:\n\n";
                message += $"✓ Successfully created: {successCount} elevation views\n";

                if (failCount > 0)
                {
                    message += $"✗ Failed to process: {failCount} elements\n";
                }

                if (results.Any())
                {
                    message += $"\nCreated Views:\n";
                    foreach (var result in results.Take(10)) // Show first 10
                    {
                        message += $"• {result.CategoryName} - {result.TypeName} (Element ID: {result.Element.Id})\n";
                    }

                    if (results.Count > 10)
                    {
                        message += $"... and {results.Count - 10} more.\n";
                    }
                }

                TaskDialog dialog = new TaskDialog("DEAXO AutoElevation Results");
                dialog.MainInstruction = "Elevation Views Created Successfully";
                dialog.MainContent = message;
                dialog.Show();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Results", $"Created {successCount} elevation views. Failed: {failCount}");
            }
        }

        private class ElevationResult
        {
            public Element Element { get; set; }
            public string CategoryName { get; set; }
            public string TypeName { get; set; }
            public ViewSheet Sheet { get; set; }
            public ViewSection ElevationView { get; set; }
        }
    }
}