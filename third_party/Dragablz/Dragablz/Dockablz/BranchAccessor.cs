using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Dragablz.Core;

namespace Dragablz.Dockablz
{ 
    public class BranchAccessor
    {
        private readonly Branch _branch;
        private readonly BranchAccessor _firstItemBranchAccessor;
        private readonly BranchAccessor _secondItemBranchAccessor;
        private readonly TabablzControl _firstItemTabablzControl;
        private readonly TabablzControl _secondItemTabablzControl;

        public BranchAccessor(Branch branch)
        {
            if (branch == null) throw new ArgumentNullException("branch");

            _branch = branch;

            var firstChildBranch = branch.FirstItem as Branch;
            if (firstChildBranch != null)
                _firstItemBranchAccessor = new BranchAccessor(firstChildBranch);
            else
                _firstItemTabablzControl = FindTabablzControl(branch.FirstItem, branch.FirstContentPresenter);

            var secondChildBranch = branch.SecondItem as Branch;            
            if (secondChildBranch != null)
                _secondItemBranchAccessor = new BranchAccessor(secondChildBranch);
            else
                _secondItemTabablzControl = FindTabablzControl(branch.SecondItem, branch.SecondContentPresenter);
        }

        private static TabablzControl FindTabablzControl(object item, DependencyObject contentPresenter)
        {
            var result = item as TabablzControl;
            return result ?? contentPresenter.VisualTreeDepthFirstTraversal().OfType<TabablzControl>().FirstOrDefault();
        }

        public Branch Branch
        {
            get { return _branch; }
        }

        public BranchAccessor FirstItemBranchAccessor
        {
            get { return _firstItemBranchAccessor; }
        }

        public BranchAccessor SecondItemBranchAccessor
        {
            get { return _secondItemBranchAccessor; }
        }

        public TabablzControl FirstItemTabablzControl
        {
            get { return _firstItemTabablzControl; }
        }

        public TabablzControl SecondItemTabablzControl
        {
            get { return _secondItemTabablzControl; }
        }

        /// <summary>
        /// Visits the content of the first or second side of a branch, according to its content type.  No more than one of the provided <see cref="Action"/>
        /// callbacks will be called.  
        /// </summary>
        /// <param name="childItem"></param>
        /// <param name="childBranchVisitor"></param>
        /// <param name="childTabablzControlVisitor"></param>
        /// <param name="childContentVisitor"></param>
        /// <returns></returns>
        public BranchAccessor Visit(BranchItem childItem,
            Action<BranchAccessor> childBranchVisitor = null,
            Action<TabablzControl> childTabablzControlVisitor = null,
            Action<object> childContentVisitor = null)
        {
            Func<BranchAccessor> branchGetter;
            Func<TabablzControl> tabGetter;
            Func<object> contentGetter;

            switch (childItem)
            {
                case BranchItem.First:
                    branchGetter = () => _firstItemBranchAccessor;
                    tabGetter = () => _firstItemTabablzControl;
                    contentGetter = () => _branch.FirstItem;
                    break;
                case BranchItem.Second:
                    branchGetter = () => _secondItemBranchAccessor;
                    tabGetter = () => _secondItemTabablzControl;
                    contentGetter = () => _branch.SecondItem;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("childItem");
            }

            var branchDescription = branchGetter();
            if (branchDescription != null)
            {
                if (childBranchVisitor != null)
                    childBranchVisitor(branchDescription);
                return this;
            }
            
            var tabablzControl = tabGetter();
            if (tabablzControl != null)
            {
                if (childTabablzControlVisitor != null)
                    childTabablzControlVisitor(tabablzControl);

                return this;
            }

            if (childContentVisitor == null) return this;

            var content = contentGetter();
            if (content != null)
                childContentVisitor(content);

            return this;
        }
    }    
}
