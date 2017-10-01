using System;

namespace Dragablz.Dockablz
{
    internal static class Finder
    {
        internal static LocationReport Find(TabablzControl tabablzControl)
        {
            if (tabablzControl == null) throw new ArgumentNullException("tabablzControl");

            var locationReportBuilder = new LocationReportBuilder(tabablzControl);            

            foreach (var loadedInstance in Layout.GetLoadedInstances())
            {
                locationReportBuilder.CurrentLayout = loadedInstance;

                loadedInstance.Query().Visit(
                    locationReportBuilder,
                    BranchVisitor,
                    TabablzControlVisitor
                    );

                if (locationReportBuilder.IsFound)
                    break;
            }

            if (!locationReportBuilder.IsFound)
                throw new LocationReportException("Instance not within any layout.");

            return locationReportBuilder.ToLocationReport();
        }

        private static void BranchVisitor(LocationReportBuilder locationReportBuilder, BranchAccessor branchAccessor)
        {
            if (Equals(branchAccessor.FirstItemTabablzControl, locationReportBuilder.TargetTabablzControl))
                locationReportBuilder.MarkFound(branchAccessor.Branch, false);
            else if (Equals(branchAccessor.SecondItemTabablzControl, locationReportBuilder.TargetTabablzControl))
                locationReportBuilder.MarkFound(branchAccessor.Branch, true);
            else
            {
                branchAccessor.Visit(BranchItem.First, ba => BranchVisitor(locationReportBuilder, ba));
                if (locationReportBuilder.IsFound) return;
                branchAccessor.Visit(BranchItem.Second, ba => BranchVisitor(locationReportBuilder, ba));
            }            
        }

        private static void TabablzControlVisitor(LocationReportBuilder locationReportBuilder, TabablzControl tabablzControl)
        {
            if (Equals(tabablzControl, locationReportBuilder.TargetTabablzControl))
                locationReportBuilder.MarkFound();
        }
    }
}