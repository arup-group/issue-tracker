using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ARUP.IssueTracker.Classes;
using ARUP.IssueTracker.Classes.BCF2;

namespace ARUP.IssueTracker.Revit.Classes
{
    public static class Utils
    {
        /// <summary>
        //MOVES THE CAMERA ACCORDING TO THE PROJECT BASE LOCATION 
        //function that changes the coordinates accordingly to the project base location to an absolute location (for BCF export)
        //if the value negative is set to true, does the opposite (for opening BCF views)
        /// </summary>
        /// <param name="c">center</param>
        /// <param name="view">view direction</param>
        /// <param name="up">up direction</param>
        /// <param name="negative">convert to/from</param>
        /// <returns></returns>
        public static ViewOrientation3D ConvertBasePoint(Document doc, XYZ c, XYZ view, XYZ up, bool negative)
        {
            //UIDocument uidoc = uiapp.ActiveUIDocument;
            //Document doc = uidoc.Document;

            //ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_ProjectBasePoint);
            //FilteredElementCollector collector = new FilteredElementCollector(doc);
            //System.Collections.Generic.IEnumerable<Element> elements = collector.WherePasses(filter).ToElements();

            double angle = 0;
            double x = 0;
            double y = 0;
            double z = 0;

            //VERY IMPORTANT
            //BuiltInParameter.BASEPOINT_EASTWEST_PARAM is the value of the BASE POINT LOCATION
            //position is the location of the BPL related to Revit's absolute origini!
            //if BPL is set to 0,0,0 not always it corresponds to Revit's origin

            ProjectLocation projectLocation = doc.ActiveProjectLocation;
            XYZ origin = new XYZ(0, 0, 0);
#if REVIT2019
            ProjectPosition position = projectLocation.GetProjectPosition(origin);

#elif REVIT2020
            ProjectPosition position = projectLocation.GetProjectPosition(origin);
#elif REVIT2021
            ProjectPosition position = projectLocation.GetProjectPosition(origin);
#elif REVIT2022
            ProjectPosition position = projectLocation.GetProjectPosition(origin);

#else
            ProjectPosition position = projectLocation.get_ProjectPosition(origin);
#endif

            int i = (negative) ? -1 : 1;
            //foreach (Element element in elements)
            //{
            //    MessageBox.Show(UnitUtils.ConvertFromInternalUnits(position.EastWest, DisplayUnitType.DUT_METERS).ToString() + "  " + element.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsValueString() + "\n" +
            //        UnitUtils.ConvertFromInternalUnits(position.NorthSouth, DisplayUnitType.DUT_METERS).ToString() + "  " + element.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsValueString() + "\n" +
            //        UnitUtils.ConvertFromInternalUnits(position.Elevation, DisplayUnitType.DUT_METERS).ToString() + "  " + element.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsValueString() + "\n" +
            //        position.Angle.ToString() + "  " + element.get_Parameter(BuiltInParameter.BASEPOINT_ANGLETON_PARAM).AsDouble().ToString());
            //}
            x = i * position.EastWest;
            y = i * position.NorthSouth;
            z = i * position.Elevation;
            angle = i * position.Angle;

            if (negative) // I do the addition BEFORE
                c = new XYZ(c.X + x, c.Y + y, c.Z + z);

            //rotation
            double centX = (c.X * Math.Cos(angle)) - (c.Y * Math.Sin(angle));
            double centY = (c.X * Math.Sin(angle)) + (c.Y * Math.Cos(angle));

            XYZ newC = new XYZ();
            if (negative)
                newC = new XYZ(centX, centY, c.Z);
            else // I do the addition AFTERWARDS
                newC = new XYZ(centX + x, centY + y, c.Z + z);


            double viewX = (view.X * Math.Cos(angle)) - (view.Y * Math.Sin(angle));
            double viewY = (view.X * Math.Sin(angle)) + (view.Y * Math.Cos(angle));
            XYZ newView = new XYZ(viewX, viewY, view.Z);

            double upX = (up.X * Math.Cos(angle)) - (up.Y * Math.Sin(angle));
            double upY = (up.X * Math.Sin(angle)) + (up.Y * Math.Cos(angle));

            XYZ newUp = new XYZ(upX, upY, up.Z);
            return new ViewOrientation3D(newC, newUp, newView);
        }

        public static XYZ ConvertToFromSharedCoordinate(Document doc, XYZ c, bool negative)
        {
            double angle = 0;
            double x = 0;
            double y = 0;
            double z = 0;

            //VERY IMPORTANT
            //BuiltInParameter.BASEPOINT_EASTWEST_PARAM is the value of the BASE POINT LOCATION
            //position is the location of the BPL related to Revit's absolute origini!
            //if BPL is set to 0,0,0 not always it corresponds to Revit's origin

            ProjectLocation projectLocation = doc.ActiveProjectLocation;
            XYZ origin = new XYZ(0, 0, 0);

#if REVIT2019
            ProjectPosition position = projectLocation.GetProjectPosition(origin);

#elif REVIT2020
            ProjectPosition position = projectLocation.GetProjectPosition(origin);
#elif REVIT2021
            ProjectPosition position = projectLocation.GetProjectPosition(origin);
#elif REVIT2022
            ProjectPosition position = projectLocation.GetProjectPosition(origin);

#else
            ProjectPosition position = projectLocation.get_ProjectPosition(origin);
#endif

            int i = (negative) ? -1 : 1;
            //foreach (Element element in elements)
            //{
            //    MessageBox.Show(UnitUtils.ConvertFromInternalUnits(position.EastWest, DisplayUnitType.DUT_METERS).ToString() + "  " + element.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsValueString() + "\n" +
            //        UnitUtils.ConvertFromInternalUnits(position.NorthSouth, DisplayUnitType.DUT_METERS).ToString() + "  " + element.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsValueString() + "\n" +
            //        UnitUtils.ConvertFromInternalUnits(position.Elevation, DisplayUnitType.DUT_METERS).ToString() + "  " + element.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsValueString() + "\n" +
            //        position.Angle.ToString() + "  " + element.get_Parameter(BuiltInParameter.BASEPOINT_ANGLETON_PARAM).AsDouble().ToString());
            //}
            x = i * position.EastWest;
            y = i * position.NorthSouth;
            z = i * position.Elevation;
            angle = i * position.Angle;

            if (negative) // I do the addition BEFORE
                c = new XYZ(c.X + x, c.Y + y, c.Z + z);

            //rotation
            double centX = (c.X * Math.Cos(angle)) - (c.Y * Math.Sin(angle));
            double centY = (c.X * Math.Sin(angle)) + (c.Y * Math.Cos(angle));

            XYZ newC = new XYZ();
            if (negative)
                newC = new XYZ(centX, centY, c.Z);
            else // I do the addition AFTERWARDS
                newC = new XYZ(centX + x, centY + y, c.Z + z);

            return newC;
        }

        public static XYZ GetInternalXYZ(double x, double y, double z)
        {

            XYZ myXYZ = new XYZ(
              UnitUtils.ConvertToInternalUnits(x, DisplayUnitType.DUT_METERS),
              UnitUtils.ConvertToInternalUnits(y, DisplayUnitType.DUT_METERS),
              UnitUtils.ConvertToInternalUnits(z, DisplayUnitType.DUT_METERS));
            return myXYZ;
        }

        public static ClippingPlane[] GetClippingPlanesFromBoundingBox(XYZ max, XYZ min, Transform toMdoelSpaceTransform, Document doc)
        {
            List<ClippingPlane> clippingPlanes = new List<ClippingPlane>();

            // transform six normals to model coordinates and shared coordinates


#if REVIT2019
            ProjectPosition projectPosition = projectLocation.GetProjectPosition(XYZ.Zero);

#elif REVIT2020
            ProjectPosition projectPosition = doc.ActiveProjectLocation.GetProjectPosition(XYZ.Zero);
#elif REVIT2021
            ProjectPosition projectPosition = doc.ActiveProjectLocation.GetProjectPosition(XYZ.Zero);
#elif REVIT2022
            ProjectPosition projectPosition = doc.ActiveProjectLocation.GetProjectPosition(XYZ.Zero);

#else
            ProjectPosition projectPosition = doc.ActiveProjectLocation.get_ProjectPosition(XYZ.Zero);
#endif




            Transform t1 = Transform.CreateTranslation(new XYZ(projectPosition.EastWest, projectPosition.NorthSouth, projectPosition.Elevation));
            Transform t2 = Transform.CreateRotation(XYZ.BasisZ, projectPosition.Angle);

            XYZ xPositiveNormalTransformed = t1.OfVector(t2.OfVector(toMdoelSpaceTransform.OfVector(new XYZ(1, 0, 0))));
            XYZ yPositiveNormalTransformed = t1.OfVector(t2.OfVector(toMdoelSpaceTransform.OfVector(new XYZ(0, 1, 0))));
            XYZ zPositiveNormalTransformed = t1.OfVector(t2.OfVector(toMdoelSpaceTransform.OfVector(new XYZ(0, 0, 1))));
            XYZ xNegativeNormalTransformed = t1.OfVector(t2.OfVector(toMdoelSpaceTransform.OfVector(new XYZ(-1, 0, 0))));
            XYZ yNegativeNormalTransformed = t1.OfVector(t2.OfVector(toMdoelSpaceTransform.OfVector(new XYZ(0, -1, 0))));
            XYZ zNegativeNormalTransformed = t1.OfVector(t2.OfVector(toMdoelSpaceTransform.OfVector(new XYZ(0, 0, -1))));            
            
            // generate BCF clipping planes
            ClippingPlane xPositive = new ClippingPlane()
            {                
                Direction = new Direction() { X = xPositiveNormalTransformed.X, Y = xPositiveNormalTransformed.Y, Z = xPositiveNormalTransformed.Z },
                Location = new ARUP.IssueTracker.Classes.BCF2.Point() { X = max.X, Y = max.Y, Z = max.Z }
            };

            ClippingPlane yPositive = new ClippingPlane()
            {
                Direction = new Direction() { X = yPositiveNormalTransformed.X, Y = yPositiveNormalTransformed.Y, Z = yPositiveNormalTransformed.Z },
                Location = new ARUP.IssueTracker.Classes.BCF2.Point() { X = max.X, Y = max.Y, Z = max.Z }
            };

            ClippingPlane zPositive = new ClippingPlane()
            {
                Direction = new Direction() { X = zPositiveNormalTransformed.X, Y = zPositiveNormalTransformed.Y, Z = zPositiveNormalTransformed.Z },
                Location = new ARUP.IssueTracker.Classes.BCF2.Point() { X = max.X, Y = max.Y, Z = max.Z }
            };

            ClippingPlane xNegative = new ClippingPlane()
            {
                Direction = new Direction() { X = xNegativeNormalTransformed.X, Y = xNegativeNormalTransformed.Y, Z = xNegativeNormalTransformed.Z },
                Location = new ARUP.IssueTracker.Classes.BCF2.Point() { X = min.X, Y = min.Y, Z = min.Z }
            };

            ClippingPlane yNegative = new ClippingPlane()
            {
                Direction = new Direction() { X = yNegativeNormalTransformed.X, Y = yNegativeNormalTransformed.Y, Z = yNegativeNormalTransformed.Z },
                Location = new ARUP.IssueTracker.Classes.BCF2.Point() { X = min.X, Y = min.Y, Z = min.Z }
            };

            ClippingPlane zNegative = new ClippingPlane()
            {
                Direction = new Direction() { X = zNegativeNormalTransformed.X, Y = zNegativeNormalTransformed.Y, Z = zNegativeNormalTransformed.Z },
                Location = new ARUP.IssueTracker.Classes.BCF2.Point() { X = min.X, Y = min.Y, Z = min.Z }
            };

            clippingPlanes.Add(xPositive);
            clippingPlanes.Add(yPositive);
            clippingPlanes.Add(zPositive);
            clippingPlanes.Add(xNegative);
            clippingPlanes.Add(yNegative);
            clippingPlanes.Add(zNegative);

            return clippingPlanes.ToArray();
        }
    }
}
