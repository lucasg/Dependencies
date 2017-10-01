namespace Dragablz
{
    public class MoveItemRequest
    {
        private readonly object _item;
        private readonly object _context;
        private readonly AddLocationHint _addLocationHint;

        public MoveItemRequest(object item, object context, AddLocationHint addLocationHint)
        {
            _item = item;
            _context = context;
            _addLocationHint = addLocationHint;
        }

        public object Item
        {
            get { return _item; }
        }

        public object Context
        {
            get { return _context; }
        }

        public AddLocationHint AddLocationHint
        {
            get { return _addLocationHint; }
        }
    }
}