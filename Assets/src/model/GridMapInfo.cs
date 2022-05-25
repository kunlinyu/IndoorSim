using System.Collections;
using System.Collections.Generic;

public struct GridMapInfo
{
    public string pngBase64;
    public double cell_x;  // coordinate of left-bottom corner cell
    public double cell_y;  // coordinate of left-bottom corner cell
    public double cell_width;  // width of each cell

    // pose of the grid map put on canvas
    public double canvas_x;
    public double canvas_y;
    public double canvas_theta;
}
