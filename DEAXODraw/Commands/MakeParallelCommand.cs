using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using DEAXODraw.Utilities;

namespace DEAXODraw.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class MakeParallelCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uidoc = uiApp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Continue processing elements until user cancels
                while (true)
                {
                    var result = ProcessElementPair(uidoc, doc);
                    if (result != Result.Succeeded)
                    {
                        break;
                    }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error in MakeParallel command: {ex.Message}";
                return Result.Failed;
            }
        }

        private Result ProcessElementPair(UIDocument uidoc, Document doc)
        {
            try
            {
                Element referenceElement = null;
                Element targetElement = null;

                // Step 1: Select reference element
                try
                {
                    TaskDialog.Show("Reference Selection",
                        "Pick the reference element (the one that stays fixed).\nPress 'Esc' to exit.");

                    var reference1 = uidoc.Selection.PickObject(ObjectType.Element, "Pick reference element");
                    referenceElement = doc.GetElement(reference1);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                // Step 2: Select target element
                try
                {
                    TaskDialog.Show("Target Selection",
                        "Pick the target element (the one that will be rotated).\nPress 'Esc' to cancel.");

                    var reference2 = uidoc.Selection.PickObject(ObjectType.Element, "Pick target element");
                    targetElement = doc.GetElement(reference2);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                // Step 3: Make elements parallel
                return MakeElementsParallel(doc, referenceElement, targetElement);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to process element pair: {ex.Message}");
                return Result.Failed;
            }
        }

        private Result MakeElementsParallel(Document doc, Element referenceElement, Element targetElement)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "DEAXO Make Parallel"))
                {
                    trans.Start();

                    // Get directions of both elements
                    XYZ referenceDirection = GetElementDirection(referenceElement, doc);
                    XYZ targetDirection = GetElementDirection(targetElement, doc);

                    if (referenceDirection == null || targetDirection == null)
                    {
                        TaskDialog.Show("Error", "Could not determine direction for one or both elements.");
                        return Result.Failed;
                    }

                    // Project directions to XY plane for parallel calculation
                    XYZ refXY = VectorUtils.ProjectToXY(referenceDirection);
                    XYZ targetXY = VectorUtils.ProjectToXY(targetDirection);

                    // Calculate angle between directions
                    double angle = targetXY.AngleTo(refXY);

                    // Choose the smaller angle (less than 90 degrees)
                    if (angle > Math.PI / 2)
                    {
                        angle = angle - Math.PI;
                    }

                    // Calculate rotation axis (cross product gives us the normal)
                    XYZ normal = targetXY.CrossProduct(refXY);
                    XYZ origin = GetElementOrigin(targetElement, doc);

                    if (origin == null)
                    {
                        TaskDialog.Show("Error", "Could not determine origin for target element.");
                        return Result.Failed;
                    }

                    // Create rotation axis
                    Line rotationAxis = VectorUtils.CreateRotationAxis(origin, normal);

                    // Handle special case for elevation views
                    Element elementToRotate = GetElementToRotate(targetElement, doc);

                    // Perform rotation
                    if (elementToRotate?.Location != null)
                    {
                        elementToRotate.Location.Rotate(rotationAxis, angle);

                        trans.Commit();

                        TaskDialog.Show("Success",
                            $"Elements are now parallel!\nRotated by {Math.Abs(angle * 180 / Math.PI):F1} degrees.");

                        return Result.Succeeded;
                    }
                    else
                    {
                        TaskDialog.Show("Error", "Target element cannot be rotated (no location property).");
                        return Result.Failed;
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to make elements parallel: {ex.Message}");
                return Result.Failed;
            }
        }

        private XYZ GetElementDirection(Element element, Document doc)
        {
            try
            {
                // Try different methods to get element direction

                // 1. Grid direction
                if (element is Grid grid)
                {
                    return grid.Curve.Direction;
                }

                // 2. Reference plane direction
                if (element is ReferencePlane refPlane)
                {
                    return refPlane.Direction;
                }

                // 3. Family instance direction
                if (element is FamilyInstance familyInstance)
                {
                    return familyInstance.FacingOrientation;
                }

                // 4. Line-based elements (walls, etc.)
                if (element.Location is LocationCurve locationCurve)
                {
                    return locationCurve.Curve.Direction;
                }

                // 5. Section view direction
                if (IsViewElement(element))
                {
                    return GetSectionDirection(element, doc);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting element direction: {ex.Message}");
                return null;
            }
        }

        private XYZ GetElementOrigin(Element element, Document doc)
        {
            try
            {
                // Try different methods to get element origin

                // 1. Grid origin
                if (element is Grid grid)
                {
                    return grid.Curve.Origin;
                }

                // 2. Reference plane origin
                if (element is ReferencePlane refPlane)
                {
                    return refPlane.GetPlane().Origin;
                }

                // 3. Family instance origin
                if (element is FamilyInstance familyInstance)
                {
                    return familyInstance.GetTransform().Origin;
                }

                // 4. Line-based elements
                if (element.Location is LocationCurve locationCurve)
                {
                    return locationCurve.Curve.Origin;
                }

                // 5. Point-based elements
                if (element.Location is LocationPoint locationPoint)
                {
                    return locationPoint.Point;
                }

                // 6. Section view origin
                if (IsViewElement(element))
                {
                    return GetSectionOrigin(element, doc);
                }

                // 7. Bounding box center as fallback
                BoundingBoxXYZ bb = element.get_BoundingBox(null);
                if (bb != null)
                {
                    return (bb.Max + bb.Min) / 2.0;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting element origin: {ex.Message}");
                return null;
            }
        }

        private Element GetElementToRotate(Element element, Document doc)
        {
            try
            {
                // For elevation views, we need to rotate the elevation marker instead
                if (IsViewElement(element) && IsElevationView(element, doc))
                {
                    return GetElevationMarker(element, doc);
                }

                return element;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting element to rotate: {ex.Message}");
                return element;
            }
        }

        private bool IsViewElement(Element element)
        {
            // Check if element has a sketch plane parameter (indicating it's a view-related element)
            Parameter sketchParam = element.get_Parameter(BuiltInParameter.VIEW_FIXED_SKETCH_PLANE);
            return sketchParam != null;
        }

        private bool IsElevationView(Element element, Document doc)
        {
            try
            {
                ViewSection view = GetViewFromElement(element, doc);
                return view?.ViewType == ViewType.Elevation;
            }
            catch
            {
                return false;
            }
        }

        private ViewSection GetViewFromElement(Element element, Document doc)
        {
            try
            {
                Parameter sketchParam = element.get_Parameter(BuiltInParameter.VIEW_FIXED_SKETCH_PLANE);
                if (sketchParam != null)
                {
                    SketchPlane sketchPlane = doc.GetElement(sketchParam.AsElementId()) as SketchPlane;
                    if (sketchPlane != null)
                    {
                        return doc.GetElement(sketchPlane.OwnerViewId) as ViewSection;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private XYZ GetSectionDirection(Element element, Document doc)
        {
            try
            {
                ViewSection view = GetViewFromElement(element, doc);
                return view?.RightDirection;
            }
            catch
            {
                return null;
            }
        }

        private XYZ GetSectionOrigin(Element element, Document doc)
        {
            try
            {
                ViewSection view = GetViewFromElement(element, doc);
                return view?.Origin;
            }
            catch
            {
                return null;
            }
        }

        private ElevationMarker GetElevationMarker(Element element, Document doc)
        {
            try
            {
                ViewSection view = GetViewFromElement(element, doc);
                if (view != null)
                {
                    var elevationMarkers = new FilteredElementCollector(doc)
                        .OfClass(typeof(ElevationMarker))
                        .Cast<ElevationMarker>();

                    foreach (var marker in elevationMarkers)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            ElementId viewId = marker.GetViewId(i);
                            if (viewId == view.Id)
                            {
                                return marker;
                            }
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}