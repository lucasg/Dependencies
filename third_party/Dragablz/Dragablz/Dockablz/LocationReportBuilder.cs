using System;

namespace Dragablz.Dockablz
{
    internal class LocationReportBuilder
    {
        private readonly TabablzControl _targetTabablzControl;
        private Branch _branch;
        private bool _isSecondLeaf;
        private Layout _layout;

        public LocationReportBuilder(TabablzControl targetTabablzControl)
        {
            _targetTabablzControl = targetTabablzControl;
        }

        public TabablzControl TargetTabablzControl
        {
            get { return _targetTabablzControl; }
        }

        public bool IsFound { get; private set; }

        public void MarkFound()
        {
            if (IsFound)
                throw new InvalidOperationException("Already found.");

            IsFound = true;

            _layout = CurrentLayout;
        }

        public void MarkFound(Branch branch, bool isSecondLeaf)
        {
            if (branch == null) throw new ArgumentNullException("branch");
            if (IsFound)
                throw new InvalidOperationException("Already found.");

            IsFound = true;

            _layout = CurrentLayout;
            _branch = branch;
            _isSecondLeaf = isSecondLeaf;
        }

        public Layout CurrentLayout { get; set; }

        public LocationReport ToLocationReport()
        {
            return new LocationReport(_targetTabablzControl, _layout, _branch, _isSecondLeaf);
        }
    }
}