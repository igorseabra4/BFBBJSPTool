using RenderWareChunk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RenderWareChunk.ReadFileMethods;

namespace BFBBJSPTool
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            Console.WriteLine("DFF Exporter Tool v0.1 by igorseabra4");
            Console.WriteLine("Usage: drag RenderWare DFF files into the program or just open it to convert every file in the folder.");
            Console.WriteLine("The files will be exported as OBJ. The material lib expects PNG files located in the same folder.");
            Console.WriteLine("Might not work with all DFF files (you'll probably have more luck with PC and XBOX files)");
            Console.WriteLine("Currently confirmed to work with (most files):");
            Console.WriteLine("Scooby-Doo: Night of 100 Frights (PS2))");
            Console.WriteLine("Shadow the Hedgehog (XBOX)");
            Console.WriteLine("Sonic Heroes (PC)");
            Console.WriteLine("Spongebob Squarepants: Battle for Bikini Bottom (XBOX) (DFF and JSP)");

            string[] Arguments = Environment.GetCommandLineArgs();
            if (Arguments.Length > 1)
            {
                foreach (string i in Arguments)
                    if (Path.GetExtension(i).ToLower() == ".jsp" | Path.GetExtension(i).ToLower() == ".dff")
                        Run(i);
            }
            else
            {
                string[] FilesInFolder = Directory.GetFiles(Directory.GetCurrentDirectory());

                foreach (string i in FilesInFolder)
                    if (Path.GetExtension(i).ToLower() == ".jsp" | Path.GetExtension(i).ToLower() == ".dff")
                        Run(i);
            }
            Console.WriteLine("Done. Press any key to close this window.");
            Console.ReadKey();
        }
        
        private static void Run(string i)
        {
            Console.WriteLine("Converting " + i);
            RWSection[] R = ReadRenderWareFile(i);
            Console.WriteLine("Exporting " + Path.ChangeExtension(i, ".obj"));
            ConvertJSPtoOBJ(i, R);
        }
        
        static int TotalVertexIndices;
        static string MaterialLibrary;
        static string FileNameWithoutExtension;
        static int untexturedMaterials;

        static public void ConvertJSPtoOBJ(string fileName, RWSection[] renderWareDFF)
        {
            TotalVertexIndices = 1;
            untexturedMaterials = 0;
            MaterialLibrary = Path.ChangeExtension(fileName, "mtl");
            FileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            StreamWriter OBJWriter = new StreamWriter((Path.ChangeExtension(fileName, "obj")), false);
            OBJWriter.WriteLine("# Exported by BFBB JSP Tool");
            OBJWriter.WriteLine("mtllib " + Path.GetFileName(MaterialLibrary));
            OBJWriter.WriteLine();

            foreach (RWSection rw in renderWareDFF)
            {
                if (rw is Clump_0010 w)
                {
                    foreach (Geometry_000F rw2 in w.geometryList.geometryList)
                    {
                        ExportGeometryToOBJ(rw2, OBJWriter);
                    }
                }
            }
            OBJWriter.Close();

            untexturedMaterials = 0;

            StreamWriter MTLWriter = new StreamWriter(MaterialLibrary, false);
            MTLWriter.WriteLine("# Exported by BFBB JSP Tool");
            MTLWriter.WriteLine();

            foreach (RWSection rw in renderWareDFF)
            {
                if (rw is Clump_0010 w)
                {
                    foreach (Geometry_000F rw2 in w.geometryList.geometryList)
                    {
                        WriteMaterialLib(rw2, MTLWriter);
                    }
                }
            }
            MTLWriter.Close();
        }

        private static void ExportGeometryToOBJ(Geometry_000F g, StreamWriter writer)
        {
            GeometryStruct_0001 gs = g.geometryStruct;

            if (g.materialList.materialList[0].materialStruct.isTextured != 0)
            {
                writer.WriteLine("g obj_" + g.materialList.materialList[0].texture.diffuseTextureName.stringString);
                writer.WriteLine("usemtl " + g.materialList.materialList[0].texture.diffuseTextureName.stringString + "_m");
            }
            else
            {
                writer.WriteLine("g obj_default_" + untexturedMaterials.ToString());
                writer.WriteLine("usemtl default_" + untexturedMaterials.ToString() + "_m");
                untexturedMaterials++;
            }
            writer.WriteLine();

            foreach (MorphTarget m in gs.morphTargets)
            {
                if (m.hasVertices != 0)
                {
                    foreach (Vertex3 v in m.vertices)
                        writer.WriteLine("v " + v.X.ToString() + " " + v.Y.ToString() + " " + v.Z.ToString());
                    writer.WriteLine();
                }

                if (m.hasNormals != 0)
                {
                    foreach (Vertex3 vn in m.normals)
                        writer.WriteLine("vn " + vn.X.ToString() + " " + vn.Y.ToString() + " " + vn.Z.ToString());
                    writer.WriteLine();
                }

                if ((gs.geometryFlags & (int)GeometryFlags.hasVertexColors) != 0)
                {
                    foreach (Color c in gs.vertexColors)
                        writer.WriteLine("vc " + c.R.ToString() + " " + c.G.ToString() + " " + c.B.ToString() + " " + c.A.ToString());
                    writer.WriteLine();
                }

                if ((gs.geometryFlags & (int)GeometryFlags.hasTextCoords) != 0)
                {
                    foreach (TextCoord tc in gs.textCoords)
                        writer.WriteLine("vt " + tc.X.ToString() + " " + tc.Y.ToString());
                    writer.WriteLine();
                }

                foreach (Triangle t in gs.triangles)
                {
                    List<char> v1 = new List<char>(8);
                    List<char> v2 = new List<char>(8);
                    List<char> v3 = new List<char>(8);

                    int n1 = t.vertex1 + TotalVertexIndices;
                    int n2 = t.vertex2 + TotalVertexIndices;
                    int n3 = t.vertex3 + TotalVertexIndices;

                    if (m.hasVertices != 0)
                    {
                        v1.AddRange(n1.ToString());
                        v2.AddRange(n2.ToString());
                        v3.AddRange(n3.ToString());
                    }
                    if (((gs.geometryFlags & (int)GeometryFlags.hasTextCoords) != 0) & (m.hasNormals != 0))
                    {
                        v1.AddRange("/" + n1.ToString() + "/" + n1.ToString());
                        v2.AddRange("/" + n2.ToString() + "/" + n2.ToString());
                        v3.AddRange("/" + n3.ToString() + "/" + n3.ToString());
                    }
                    else if ((gs.geometryFlags & (int)GeometryFlags.hasTextCoords) != 0)
                    {
                        v1.AddRange("/" + n1.ToString());
                        v2.AddRange("/" + n2.ToString());
                        v3.AddRange("/" + n3.ToString());
                    }
                    else if (m.hasNormals != 0)
                    {
                        v1.AddRange("//" + n1.ToString());
                        v2.AddRange("//" + n2.ToString());
                        v3.AddRange("//" + n3.ToString());
                    }
                    writer.WriteLine("f " + new string(v1.ToArray()) + " " + new string(v2.ToArray()) + " " + new string(v3.ToArray()));
                }

                TotalVertexIndices += m.vertices.Count();
                writer.WriteLine();
            }
        }

        private static void WriteMaterialLib(Geometry_000F g, StreamWriter MTLWriter)
        {
            string textureName;
            if (g.materialList.materialList[0].materialStruct.isTextured != 0)
            {
                textureName = g.materialList.materialList[0].texture.diffuseTextureName.stringString;
            }
            else
            {
                textureName = "default_" + untexturedMaterials.ToString();
                untexturedMaterials++;
            }

            MTLWriter.WriteLine("newmtl " + textureName + "_m");
            MTLWriter.WriteLine("Ka 0.2 0.2 0.2");
            MTLWriter.WriteLine("Kd 0.8 0.8 0.8");
            MTLWriter.WriteLine("Ks 0 0 0");
            MTLWriter.WriteLine("Ns 10");
            MTLWriter.WriteLine("d 1.0");
            MTLWriter.WriteLine("illum 4");
            if (g.materialList.materialList[0].materialStruct.isTextured != 0)
                MTLWriter.WriteLine("map_Kd " + textureName + ".png");
            MTLWriter.WriteLine();
        }
    }
}
