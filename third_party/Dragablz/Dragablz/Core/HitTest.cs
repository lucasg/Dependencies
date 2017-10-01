namespace Dragablz.Core
{
    /// <summary>
    /// Non-client hit test values, HT*
    /// </summary>
    internal enum HitTest
    {
        HT_ERROR = -2,
        HT_TRANSPARENT = -1,
        HT_NOWHERE = 0,
        HT_CLIENT = 1,
        HT_CAPTION = 2,
        HT_SYSMENU = 3,
        HT_GROWBOX = 4,
        HT_MENU = 5,
        HT_HSCROLL = 6,
        HT_VSCROLL = 7,
        HT_MINBUTTON = 8,
        HT_MAXBUTTON = 9,
        HT_LEFT = 10,
        HT_RIGHT = 11,
        HT_TOP = 12,
        HT_TOPLEFT = 13,
        HT_TOPRIGHT = 14,
        HT_BOTTOM = 15,
        HT_BOTTOMLEFT = 16,
        HT_BOTTOMRIGHT = 17,
        HT_BORDER = 18,
        HT_OBJECT = 19,
        HT_CLOSE = 20,
        HT_HELP = 21
    }
}