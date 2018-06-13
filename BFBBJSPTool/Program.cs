using RenderWareFile;
using RenderWareFile.Sections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFBBJSPTool
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            Console.WriteLine("BFBB JSP Tool v0.2 by igorseabra4");
            Console.WriteLine("Usage: drag JSP files from Spongebob Squarepants: Battle for Bikini Bottom (XBOX) into the program or just open it to convert every file in the folder.");
            Console.WriteLine("The files will be exported as OBJ. The material lib expects PNG files located in the same folder.");
            Console.WriteLine("Should work with some DFF files (MODL) as well.");
            //Console.WriteLine("Currently confirmed to work with (most files):");
            //Console.WriteLine("Scooby-Doo: Night of 100 Frights (PS2))");
            //Console.WriteLine("Shadow the Hedgehog (XBOX)");
            //Console.WriteLine("Sonic Heroes (PC)");
            //Console.WriteLine("Spongebob Squarepants: Battle for Bikini Bottom (XBOX) (DFF and JSP)");

            string[] filesToConvert = Environment.GetCommandLineArgs();
            bool ignoreExtension = true;

            if (filesToConvert.Length <= 1)
            {
                filesToConvert = Directory.GetFiles(Directory.GetCurrentDirectory());
                ignoreExtension = false;
            }
            
            foreach (string i in filesToConvert)
                if ((Path.GetExtension(i).ToLower() == ".jsp" | Path.GetExtension(i).ToLower() == ".dff") | ignoreExtension)
                    Convert(i);

            Console.WriteLine("Done. Press any key to close this window.");
            Console.ReadKey();
        }
        
        private static void Convert(string i)
        {
            Console.WriteLine("Converting " + i);
            RWSection[] R = ReadFileMethods.ReadRenderWareFile(i);
            Console.WriteLine("Exporting " + Path.ChangeExtension(i, ".obj"));
            ConvertJSPtoOBJ(i, R);
        }

        static int totalVertexIndices;

        static public void ConvertJSPtoOBJ(string fileName, RWSection[] renderWareFile)
        {
            totalVertexIndices = 1;

            string materialLibrary = Path.ChangeExtension(fileName, "MTL");
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            int untexturedMaterials = 0;

            StreamWriter writer = new StreamWriter((Path.ChangeExtension(fileName, "obj")), false);
            writer.WriteLine("# Exported by BFBB JSP Tool");
            writer.WriteLine("mtllib " + Path.GetFileName(materialLibrary));
            writer.WriteLine();

            foreach (RWSection rw in renderWareFile)
            {
                if (rw is Clump_0010 w)
                {
                    foreach (Geometry_000F rw2 in w.geometryList.geometryList)
                    {
                        ExportGeometryToOBJ(writer, rw2, ref untexturedMaterials);
                    }
                }
            }

            writer.Close();

            untexturedMaterials = 0;

            StreamWriter MTLWriter = new StreamWriter(materialLibrary, false);
            MTLWriter.WriteLine("# Exported by BFBB JSP Tool");
            MTLWriter.WriteLine();

            foreach (RWSection rw in renderWareFile)
            {
                if (rw is Clump_0010 w)
                {
                    foreach (Geometry_000F rw2 in w.geometryList.geometryList)
                    {
                        WriteMaterialLib(rw2, MTLWriter, ref untexturedMaterials);
                    }
                }
            }
            MTLWriter.Close();
        }
        
        private static void ExportGeometryToOBJ(StreamWriter writer, Geometry_000F g, ref int untexturedMaterials)
        {
            List<string> MaterialList = new List<string>();
            foreach (Material_0007 m in g.materialList.materialList)
            {
                if (m.texture != null)
                {
                    string textureName = m.texture.diffuseTextureName.stringString;
                    //if (!MaterialList.Contains(textureName))
                    MaterialList.Add(textureName);
                }
                else
                    MaterialList.Add("default");
            }

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

            if (gs.geometryFlags2 == 0x0101)
            {
                WriteNativeData(writer, g);
                return;
            }

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

                    int n1 = t.vertex1 + totalVertexIndices;
                    int n2 = t.vertex2 + totalVertexIndices;
                    int n3 = t.vertex3 + totalVertexIndices;

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

                totalVertexIndices += m.vertices.Count();
                writer.WriteLine();
            }
        }
        
        private static void WriteNativeData(StreamWriter writer, Geometry_000F g)
        {
            NativeDataGC n = null;

            foreach (RWSection rw in g.geometryExtension.extensionSectionList)
            {
                if (rw is BinMeshPLG_050E binmesh)
                {
                    if (binmesh.numMeshes == 0) return;
                }
                if (rw is NativeDataPLG_0510 native)
                {
                    n = native.nativeDataStruct.nativeData;
                    break;
                }
            }

            if (n == null) throw new Exception();

            List<Vertex3> vertexList_init = new List<Vertex3>();
            List<Color> colorList_init = new List<Color>();
            List<TextCoord> textCoordList_init = new List<TextCoord>();
            List<Vertex3> normalList_init = new List<Vertex3>();
            List<Triangle> triangleList = new List<Triangle>();

            foreach (Declaration d in n.declarations)
            {
                if (d.declarationType == Declarations.Vertex)
                {
                    foreach (Vertex3 v in d.entryList)
                        vertexList_init.Add(v);
                }
                else if (d.declarationType == Declarations.Color)
                {
                    foreach (Color c in d.entryList)
                        colorList_init.Add(c);
                }
                else if (d.declarationType == Declarations.TextCoord)
                {
                    foreach (TextCoord t in d.entryList)
                        textCoordList_init.Add(t);
                }
                else if (d.declarationType == Declarations.Normal)
                {
                    foreach (Vertex3 v in d.entryList)
                        normalList_init.Add(v);
                }
                else throw new Exception();
            }

            foreach (TriangleDeclaration td in n.triangleDeclarations)
            {
                foreach (TriangleList tl in td.TriangleListList)
                {
                    List<Vertex3> vertexList_final = new List<Vertex3>();
                    List<Color> colorList_final = new List<Color>();
                    List<TextCoord> textCoordList_final = new List<TextCoord>();
                    List<Vertex3> normalList_final = new List<Vertex3>();

                    foreach (int[] objectList in tl.entries)
                    {
                        for (int j = 0; j < objectList.Count(); j++)
                        {
                            if (n.declarations[j].declarationType == Declarations.Vertex)
                            {
                                vertexList_final.Add(vertexList_init[objectList[j]]);
                            }
                            else if (n.declarations[j].declarationType == Declarations.Color)
                            {
                                colorList_final.Add(colorList_init[objectList[j]]);
                            }
                            else if (n.declarations[j].declarationType == Declarations.TextCoord)
                            {
                                textCoordList_final.Add(textCoordList_init[objectList[j]]);
                            }
                            else if (n.declarations[j].declarationType == Declarations.Normal)
                            {
                                normalList_final.Add(normalList_init[objectList[j]]);
                            }
                            else throw new Exception();
                        }
                    }

                    bool control = true;

                    for (int i = 2; i < vertexList_final.Count(); i++)
                    {
                        if (control)
                        {
                            triangleList.Add(new Triangle
                            {                                
                                materialIndex = (ushort)td.MaterialIndex,
                                vertex1 = (ushort)(i - 2),
                                vertex2 = (ushort)(i - 1),
                                vertex3 = (ushort)(i)
                            });
                        }
                        else
                        {
                            triangleList.Add(new Triangle
                            {
                                materialIndex = (ushort)td.MaterialIndex,
                                vertex1 = (ushort)(i - 2),
                                vertex2 = (ushort)(i),
                                vertex3 = (ushort)(i - 1)
                            });
                        }

                        control = !control;
                    }

                    //Write vertex list to obj
                    foreach (Vertex3 i in vertexList_final)
                        writer.WriteLine("v " + i.X.ToString() + " " + i.Y.ToString() + " " + i.Z.ToString());
                    writer.WriteLine();

                    //Write uv list to obj
                    if (textCoordList_final.Count() > 0)
                        foreach (TextCoord i in textCoordList_final)
                            writer.WriteLine("vt " + i.X.ToString() + " " + (-i.Y).ToString());
                    writer.WriteLine();

                    //Write normal list to obj
                    if (normalList_final.Count() > 0)
                        foreach (Vertex3 i in normalList_final)
                            writer.WriteLine("vn " + i.X.ToString() + " " + i.Y.ToString() + " " + i.Z.ToString());
                    writer.WriteLine();

                    // Write vcolors to obj
                    if (colorList_final.Count() > 0)
                        foreach (Color i in colorList_final)
                            writer.WriteLine("vc " + i.R.ToString() + " " + i.G.ToString() + " " + i.B.ToString() + " " + i.A.ToString());
                    writer.WriteLine();

                    foreach (Triangle t in triangleList)
                    {
                        List<char> v1 = new List<char>(8);
                        List<char> v2 = new List<char>(8);
                        List<char> v3 = new List<char>(8);

                        int n1 = t.vertex1 + totalVertexIndices;
                        int n2 = t.vertex2 + totalVertexIndices;
                        int n3 = t.vertex3 + totalVertexIndices;

                        v1.AddRange(n1.ToString());
                        v2.AddRange(n2.ToString());
                        v3.AddRange(n3.ToString());

                        if (((g.geometryStruct.geometryFlags & (int)GeometryFlags.hasTextCoords) != 0) & (g.geometryStruct.geometryFlags & (int)GeometryFlags.hasNormals) != 0)
                        {
                            v1.AddRange("/" + n1.ToString() + "/" + n1.ToString());
                            v2.AddRange("/" + n2.ToString() + "/" + n2.ToString());
                            v3.AddRange("/" + n3.ToString() + "/" + n3.ToString());
                        }
                        else if ((g.geometryStruct.geometryFlags & (int)GeometryFlags.hasTextCoords) != 0)
                        {
                            v1.AddRange("/" + n1.ToString());
                            v2.AddRange("/" + n2.ToString());
                            v3.AddRange("/" + n3.ToString());
                        }
                        else if ((g.geometryStruct.geometryFlags & (int)GeometryFlags.hasNormals) != 0)
                        {
                            v1.AddRange("//" + n1.ToString());
                            v2.AddRange("//" + n2.ToString());
                            v3.AddRange("//" + n3.ToString());
                        }
                        writer.WriteLine("f " + new string(v1.ToArray()) + " " + new string(v2.ToArray()) + " " + new string(v3.ToArray()));
                    }

                    writer.WriteLine();

                    totalVertexIndices += vertexList_final.Count();
                }
            }
        }
        
        private static void WriteMaterialLib(Geometry_000F g, StreamWriter MTLWriter, ref int untexturedMaterials)
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
