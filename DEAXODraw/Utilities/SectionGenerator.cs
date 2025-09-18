using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace DEAXODraw.Utilities
{
    public class SectionGenerator
    {
        private Document _doc;
        private XYZ _origin;
        private XYZ _vector;
        private double _width;
        private double _height;
        private double _offset;
        private double _depth;
        private double _depthOffset;

        public SectionGenerator(Document doc, XYZ origin, XYZ vector, double width, double height,
            double offset = 1.0, double depth = 1.0, double depthOffset = 1.0)
        {
            _doc = doc;
            _origin = origin;
            _vector = vector.Normalize();
            _width = width;
            _height = height;
            _offset = offset;
            _depth = depth;
            _depthOffset = depthOffset;
        }

        /// <summary>
        /// Creates section views (elevation, cross-section, plan) for the element
        /// </summary>
        /// <param name="viewNameBase">Base name for the views</param>
        /// <returns>Tuple containing (elevation, cross-section, plan) views</returns>
        public (ViewSection elevation, ViewSection crossSection, ViewPlan plan) CreateSections(string viewNameBase)
        {
            try
            {
                ViewSection elevation = CreateElevationView(viewNameBase);
                ViewSection crossSection = CreateCrossSectionView(viewNameBase);
                ViewPlan plan = CreatePlanView(viewNameBase);

                return (elevation, crossSection, plan);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating sections: {ex.Message}");
                return (null, null, null);
            }
        }

        /// <summary>
        /// Creates only an elevation view
        /// </summary>
        /// <param name="viewNameBase">Base name for the view</param>
        /// <returns>Elevation view</returns>
        public ViewSection CreateElevationOnly(string viewNameBase)
        {
            try
            {
                return CreateElevationView(viewNameBase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating elevation: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates only a cross-section view
        /// </summary>
        /// <param name="viewNameBase">Base name for the view</param>
        /// <returns>Cross-section view</returns>
        public ViewSection CreateCrossSectionOnly(string viewNameBase)
        {
            try
            {
                return CreateCrossSectionView(viewNameBase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating cross-section: {ex.Message}");
                return null;
            }
        }

        private ViewSection CreateElevationView(string viewNameBase)
        {
            // For elevation, we look along the element's vector (parallel to its length)
            XYZ viewDirection = _vector;
            XYZ upDirection = XYZ.BasisZ;
            XYZ rightDirection = upDirection.CrossProduct(viewDirection).Normalize();

            // Create transform for the section view
            Transform transform = Transform.CreateLookAt(_origin, _origin + viewDirection, upDirection);

            // Create bounding box for the section
            BoundingBoxXYZ sectionBox = CreateSectionBoundingBox(rightDirection, upDirection, viewDirection);

            // Get section view type
            ViewFamilyType sectionViewType = GetSectionViewType();
            if (sectionViewType == null) return null;

            // Create the section view
            ViewSection elevationView = ViewSection.CreateSection(_doc, sectionViewType.Id, sectionBox, transform);

            // Set view name
            try
            {
                elevationView.Name = $"{viewNameBase}_Elevation";
            }
            catch
            {
                elevationView.Name = $"{viewNameBase}_Elevation_{elevationView.Id}";
            }

            return elevationView;
        }

        private ViewSection CreateCrossSectionView(string viewNameBase)
        {
            // For cross-section, we look perpendicular to the element's vector
            XYZ rightDirection = _vector;
            XYZ viewDirection = XYZ.BasisZ.CrossProduct(rightDirection).Normalize();
            XYZ upDirection = XYZ.BasisZ;

            // Create transform for the section view
            Transform transform = Transform.CreateLookAt(_origin, _origin + viewDirection, upDirection);

            // Create bounding box for the section
            BoundingBoxXYZ sectionBox = CreateSectionBoundingBox(rightDirection, upDirection, viewDirection);

            // Get section view type
            ViewFamilyType sectionViewType = GetSectionViewType();
            if (sectionViewType == null) return null;

            // Create the section view
            ViewSection crossSectionView = ViewSection.CreateSection(_doc, sectionViewType.Id, sectionBox, transform);

            // Set view name
            try
            {
                crossSectionView.Name = $"{viewNameBase}_CrossSection";
            }
            catch
            {
                crossSectionView.Name = $"{viewNameBase}_CrossSection_{crossSectionView.Id}";
            }

            return crossSectionView;
        }

        private ViewPlan CreatePlanView(string viewNameBase)
        {
            try
            {
                // Get floor plan view type
                ViewFamilyType planViewType = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(x => x.ViewFamily == ViewFamily.FloorPlan);

                if (planViewType == null) return null;

                // Find a level to associate with the plan view
                Level level = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(x => x.Elevation)
                    .FirstOrDefault();

                if (level == null) return null;

                // Create the plan view
                ViewPlan planView = ViewPlan.Create(_doc, planViewType.Id, level.Id);

                // Set view name
                try
                {
                    planView.Name = $"{viewNameBase}_Plan";
                }
                catch
                {
                    planView.Name = $"{viewNameBase}_Plan_{planView.Id}";
                }

                return planView;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating plan view: {ex.Message}");
                return null;
            }
        }

        private BoundingBoxXYZ CreateSectionBoundingBox(XYZ rightDirection, XYZ upDirection, XYZ viewDirection)
        {
            BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();

            // Calculate extents
            double halfWidth = _width / 2.0 + _offset;
            double halfHeight = _height / 2.0 + _offset;
            double halfDepth = _depth / 2.0 + _depthOffset;

            // Set minimum and maximum points
            sectionBox.Min = new XYZ(-halfWidth, -halfDepth, -halfHeight);
            sectionBox.Max = new XYZ(halfWidth, halfDepth, halfHeight);

            return sectionBox;
        }

        private ViewFamilyType GetSectionViewType()
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(x => x.ViewFamily == ViewFamily.Section);
        }
    }
}