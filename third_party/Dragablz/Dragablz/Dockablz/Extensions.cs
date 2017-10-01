using System;
using System.Runtime.Serialization;

namespace Dragablz.Dockablz
{
    public static class Extensions
    {
        /// <summary>
        /// Begin a query of layout content, returning an accessor for examining the children.
        /// </summary>
        /// <param name="layout"></param>
        /// <returns></returns>
        public static LayoutAccessor Query(this Layout layout)
        {
            if (layout == null) throw new ArgumentNullException("layout");

            return new LayoutAccessor(layout);
        }

        /// <summary>
        /// Helper method for <see cref="LayoutAccessor.Visit"/> which allows a context to be passed through.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="layoutAccessor"></param>
        /// <param name="context"></param>
        /// <param name="branchVisitor"></param>
        /// <param name="tabablzControlVisitor"></param>
        /// <param name="contentVisitor"></param>
        /// <returns></returns>
        public static LayoutAccessor Visit<TContext>(
            this LayoutAccessor layoutAccessor,
            TContext context,
            Action<TContext, BranchAccessor> branchVisitor = null,
            Action<TContext, TabablzControl> tabablzControlVisitor = null,
            Action<TContext, object> contentVisitor = null)
        {
            if (layoutAccessor == null) throw new ArgumentNullException("layoutAccessor");

            layoutAccessor.Visit(
                WrapVisitor(context, branchVisitor),
                WrapVisitor(context, tabablzControlVisitor),
                WrapVisitor(context, contentVisitor)
                );

            return layoutAccessor;
        }

        /// <summary>
        /// Helper method for <see cref="BranchAccessor.Visit"/> which allows a context to be passed through.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="branchAccessor"></param>
        /// <param name="context"></param>
        /// <param name="childItem"></param>
        /// <param name="branchVisitor"></param>
        /// <param name="tabablzControlVisitor"></param>
        /// <param name="contentVisitor"></param>
        /// <returns></returns>
        public static BranchAccessor Visit<TContext>(
            this BranchAccessor branchAccessor,
            TContext context,
            BranchItem childItem,
            Action<TContext, BranchAccessor> branchVisitor = null,
            Action<TContext, TabablzControl> tabablzControlVisitor = null,
            Action<TContext, object> contentVisitor = null)
        {
            if (branchAccessor == null) throw new ArgumentNullException("branchAccessor");

            branchAccessor.Visit(
                childItem,
                WrapVisitor(context, branchVisitor),
                WrapVisitor(context, tabablzControlVisitor),
                WrapVisitor(context, contentVisitor)
                );

            return branchAccessor;
        }

        private static Action<TVisitArg> WrapVisitor<TContext, TVisitArg>(TContext context, Action<TContext, TVisitArg> visitor)
        {
            if (visitor == null) return null;

            return a => visitor(context, a);
        }
    }
}