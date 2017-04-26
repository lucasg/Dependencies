namespace WPF.MDI
{
	/// <summary>
	/// Specifies the layout of MdiChild windows in an MdiContainer control.
	/// </summary>
	public enum MdiLayout
	{
		/// <summary>
		/// All MdiChild windows are cascaded within the client region of the MdiContainer control.
		/// </summary>
		Cascade,
		/// <summary>
		/// All MdiChild windows are tiled horizontally within the client region of the MdiContainer control.
		/// </summary>
		TileHorizontal,
		/// <summary>
		/// All MdiChild windows are tiled vertically within the client region of the MdiContainer control.
		/// </summary>
		TileVertical,
		/// <summary>
		/// All MdiChild icons are arranged within the client region of the MdiContainer control.
		/// </summary>
		ArrangeIcons
	}
}
