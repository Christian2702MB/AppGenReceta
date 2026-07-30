using System;
using System.Collections.Generic;
using AppGenReceta.DA;

class Program
{
    static void Main()
    {
        var da = new Liquidacion_DA();
        var items = da.ObtenerItemsActivosNP("i8505-V4-ES050437");
        Console.WriteLine("Items:");
        foreach(var itm in items) Console.WriteLine(itm);
        
        var versiones = da.ObtenerVersionesNP("i8505-V4-ES050437");
        Console.WriteLine("Versiones:");
        foreach(var v in versiones) Console.WriteLine(v);
    }
}
