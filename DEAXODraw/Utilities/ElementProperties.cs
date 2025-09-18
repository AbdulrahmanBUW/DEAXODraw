using System;
using Autodesk.Revit.DB;
using DEAXODraw.Utilities;

namespace DEAXODraw.Utilities
{
    public class ElementProperties
    {
        public XYZ Origin { get; private set; }
        public XYZ Vector { get; private set; }
        public double Width { get; private set; }
        public double Height { get; private set; }
        public double Depth { get; private set; }

        public double Offset { get; set; } = 1.0;
        public double DepthOffset { get; set; } = 1.0;

        public bool IsValid { get; private set; }

        private Element _element;
        private Document _doc;

        public ElementProperties(Element element, Document doc)
        {
            _element = element;
            _doc = doc;

            if (element is Wall wall)
            {
                GetWallProperties(wall);
            }
            else
            {
                GetGenericProperties();
            }
        }

        private void GetWallProperties(Wall wall)
        {
            try
            {
                Curve wallCurve = (wall.Location as LocationCurve).Curve;
                XYZ startPoint = wallCurve.GetEndPoint(0);
                XYZ endPoint = wallCurve.GetEndPoint(1);

                Vector = endPoint - startPoint;
                Width = Vector.GetLength();

                Parameter heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                Height = heightParam?.AsDouble() ?? 10.0; // Default height if parameter not found

                BoundingBoxXYZ bb = wall.get_BoundingBox(null);
                if (bb != null)
                {
                    Origin = (bb.Max + bb.Min) / 2.0;
                }

                IsValid = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting wall properties: {ex.Message}");
                IsValid = false;
            }
        }

        private void GetGenericProperties()
        {
            try
            {
                ElementType elementType = _doc.GetElement(_element.GetTypeId()) as ElementType;
                BoundingBoxXYZ bb = _element.get_BoundingBox(null);
                BoundingBoxXYZ typeBB = elementType?.get_BoundingBox(null);

                if (bb == null)
                {
                    IsValid = false;
                    return;
                }

                Origin = (bb.Max + bb.Min) / 2.0;

                if (_element is FamilyInstance familyInstance)
                {
                    GetFamilyInstanceProperties(familyInstance, elementType, bb, typeBB);
                }
                else
                {
                    // Generic element properties
                    if (typeBB != null)
                    {
                        Width = typeBB.Max.X - typeBB.Min.X;
                        Height = typeBB.Max.Z - typeBB.Min.Z;
                        Depth = typeBB.Max.Y - typeBB.Min.Y;
                    }
                    else
                    {
                        Width = bb.Max.X - bb.Min.X;
                        Height = bb.Max.Z - bb.Min.Z;
                        Depth = bb.Max.Y - bb.Min.Y;
                    }

                    XYZ startPoint = new XYZ(bb.Min.X, (bb.Min.Y + bb.Max.Y) / 2, bb.Min.Z);
                    XYZ endPoint = new XYZ(bb.Max.X, (bb.Min.Y + bb.Max.Y) / 2, bb.Min.Z);
                    Vector = endPoint - startPoint;
                }

                IsValid = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting generic properties: {ex.Message}");
                IsValid = false;
            }
        }

        private void GetFamilyInstanceProperties(FamilyInstance familyInstance, ElementType elementType,
            BoundingBoxXYZ bb, BoundingBoxXYZ typeBB)
        {
            try
            {
                FamilySymbol familySymbol = elementType as FamilySymbol;
                Family family = familySymbol?.Family;

                if (family != null)
                {
                    FamilyPlacementType placementType = family.FamilyPlacementType;

                    switch (placementType)
                    {
                        case FamilyPlacementType.OneLevelBased:
                        case FamilyPlacementType.TwoLevelsBased:
                        case FamilyPlacementType.WorkPlaneBased:
                            GetPointBasedProperties(familyInstance, typeBB);
                            break;

                        case FamilyPlacementType.CurveBased:
                        case FamilyPlacementType.CurveDrivenStructural:
                            GetCurveBasedProperties(familyInstance, bb);
                            break;

                        case FamilyPlacementType.OneLevelBasedHosted:
                            GetHostedProperties(familyInstance, typeBB, bb);
                            break;

                        default:
                            GetPointBasedProperties(familyInstance, typeBB);
                            break;
                    }
                }
                else
                {
                    GetDefaultProperties(bb, typeBB);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting family instance properties: {ex.Message}");
                GetDefaultProperties(bb, typeBB);
            }
        }

        private void GetPointBasedProperties(FamilyInstance familyInstance, BoundingBoxXYZ typeBB)
        {
            if (typeBB != null)
            {
                Width = typeBB.Max.X - typeBB.Min.X;
                Height = typeBB.Max.Z - typeBB.Min.Z;
                Depth = typeBB.Max.Y - typeBB.Min.Y;

                XYZ startPoint = new XYZ(typeBB.Min.X, (typeBB.Min.Y + typeBB.Max.Y) / 2, typeBB.Min.Z);
                XYZ endPoint = new XYZ(typeBB.Max.X, (typeBB.Min.Y + typeBB.Max.Y) / 2, typeBB.Min.Z);
                Vector = endPoint - startPoint;

                // Apply rotation if available
                LocationPoint location = familyInstance.Location as LocationPoint;
                if (location != null)
                {
                    double rotation = location.Rotation;
                    Vector = VectorUtils.RotateVector(Vector, rotation);
                }
            }
        }

        private void GetCurveBasedProperties(FamilyInstance familyInstance, BoundingBoxXYZ bb)
        {
            LocationCurve locationCurve = familyInstance.Location as LocationCurve;
            if (locationCurve?.Curve != null)
            {
                Curve curve = locationCurve.Curve;
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);

                // Ensure Z coordinates match
                if (Math.Abs(startPoint.Z - endPoint.Z) > 1e-6)
                {
                    endPoint = new XYZ(endPoint.X, endPoint.Y, startPoint.Z);
                }

                Vector = endPoint - startPoint;
                Width = Vector.GetLength();
                Height = bb.Max.Z - bb.Min.Z;
            }
        }

        private void GetHostedProperties(FamilyInstance familyInstance, BoundingBoxXYZ typeBB, BoundingBoxXYZ bb)
        {
            Element host = familyInstance.Host;

            if (host is Wall hostWall)
            {
                Curve wallCurve = (hostWall.Location as LocationCurve).Curve;
                XYZ startPoint = wallCurve.GetEndPoint(0);
                XYZ endPoint = wallCurve.GetEndPoint(1);
                Vector = endPoint - startPoint;

                // Check if facing is flipped
                try
                {
                    if (familyInstance.FacingFlipped)
                    {
                        Vector = -Vector;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error checking facing flip: {ex.Message}");
                }

                if (typeBB != null)
                {
                    Width = typeBB.Max.X - typeBB.Min.X;
                    Height = typeBB.Max.Z - typeBB.Min.Z;
                }
            }
        }

        private void GetDefaultProperties(BoundingBoxXYZ bb, BoundingBoxXYZ typeBB)
        {
            if (typeBB != null)
            {
                Width = typeBB.Max.X - typeBB.Min.X;
                Height = typeBB.Max.Z - typeBB.Min.Z;
                Depth = typeBB.Max.Y - typeBB.Min.Y;
            }
            else
            {
                Width = bb.Max.X - bb.Min.X;
                Height = bb.Max.Z - bb.Min.Z;
                Depth = bb.Max.Y - bb.Min.Y;
            }

            XYZ startPoint = new XYZ(bb.Min.X, (bb.Min.Y + bb.Max.Y) / 2, bb.Min.Z);
            XYZ endPoint = new XYZ(bb.Max.X, (bb.Min.Y + bb.Max.Y) / 2, bb.Min.Z);
            Vector = endPoint - startPoint;
        }
    }
}