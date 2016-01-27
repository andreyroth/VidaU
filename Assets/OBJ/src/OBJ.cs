//OBJ import for Unity3d 1/3
//OBJ.cs by bartek drozdz of http://www.everyday3d.com (ammendments by Jon Martin of jon-martin.com & fusedworks.com)
 
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
 
public class OBJ : MonoBehaviour {
   
    public string objPath;
   
    /* OBJ file tags */
    private const string O  = "o";
    private const string G  = "g";
    private const string V  = "v";
    private const string VT = "vt";
    private const string VN = "vn";
    private const string F  = "f";
    private const string MTL = "mtllib";
    private const string UML = "usemtl";

    /* MTL file tags */
    private const string NML = "newmtl";
    private const string NS = "Ns"; // Shininess
    private const string KA = "Ka"; // Ambient component (not supported)
    private const string KD = "Kd"; // Diffuse component
    private const string KS = "Ks"; // Specular component
    private const string D = "d";   // Transparency (not supported)
    private const string TR = "Tr"; // Same as 'd'
    private const string ILLUM = "illum"; // Illumination model. 1 - diffuse, 2 - specular
    private const string MAP_KD = "map_Kd"; // Diffuse texture (other textures are not supported)
   
    private string basepath;
    private string mtllib;
    private GeometryBuffer buffer;
	
	GameObject text;
	
    void Start ()
    {
		text = GameObject.Find("Text");
		/* Start of modifyed code
		* Andrey Roth Ehrenberg
		* Centro Universitário Senac - 2013
		* Programa de iniciaçao científica
		*/
		//String do Caminho completo
			string caminhoCompleto = Application.dataPath;
		//Pega o caminhoCompleto e corta as duas ﾃｺ últimas barras e assimila esse valor ao caminhoBase
			//Se for para a versao de desenvolvimento (Debug)
		string caminhoBase = (caminhoCompleto.IndexOf("/") == -1) ? "" : caminhoCompleto.Substring(0, caminhoCompleto.LastIndexOf("/") + 1);
			//Se for para a versao compilada
		/*
		string caminhoBase = "";
		if(caminhoCompleto.IndexOf('/') == -1){
			caminhoBase = "";		
		}
		
		else {
		Debug.Log(caminhoCompleto);
			caminhoCompleto = caminhoCompleto.Substring(0, caminhoCompleto.LastIndexOf("/"));
			//caminhoBase = caminhoCompleto.Substring(0, caminhoCompleto.LastIndexOf("/"));
			caminhoBase = caminhoCompleto;
		Debug.Log(caminhoBase);
		}
		*/
		//Obtém a informaçao do caminho base
		DirectoryInfo dir = new DirectoryInfo(caminhoBase);
		//Captura todos os arquivos com .obj no final
		FileInfo[] info = dir.GetFiles("*.obj*");
		//Para cada arquivo em info:
		objPath = info[0].FullName;
		//objPath = info[0].DirectoryName+'/'+info[0].Name;
		//text.guiText.text = caminhoBase;
	 	//End of the modifyed code

        buffer = new GeometryBuffer ();
        //ALTERATION 0:- To Cater for Local Files.
        if (!objPath.Contains("http://")) {
                basepath=Path.GetDirectoryName(objPath);
                objPath="file:///"+objPath;
                basepath="file:///"+basepath+Path.DirectorySeparatorChar;
        } else {
                basepath = (objPath.IndexOf("/") == -1) ? "" : objPath.Substring(0, objPath.LastIndexOf("/") + 1);
        }
        StartCoroutine (Load (objPath));
    }
   
    public IEnumerator Load(string path) {
            WWW loader = new WWW(path);
            yield return loader;
            SetGeometryData(loader.text);
           
            if(hasMaterials) {
                    loader = new WWW(basepath + mtllib);
                    yield return loader;
                   
                    //ALTERATION 4:- Lets say the user moved the material library elsewhere
                    //See also new function in GeometryBuffer ReturnMaterialNames()
                    //and correction to OBJ.GetMaterial() to cater for this.
                    if (loader.error != null) {
                            //Could allow user to choose new path here for material library or create alert!
                            string[] emats=buffer.ReturnMaterialNames(); //retrieves material names from groups
                            // builds material data with defaults
                            materialData = new List<MaterialData>();
                            MaterialData nmd = new MaterialData();
                            foreach(string mname in emats) {
                                    nmd = new MaterialData();
                                    nmd.name = mname;
                                    nmd.diffuse = new Color(0.5f, 0.5f, 0.5f, 1.0f);
                                    materialData.Add(nmd);
                            }
                           
                           
                    } else {
                            SetMaterialData(loader.text);                          
                            foreach(MaterialData m in materialData) {
                                    if(m.diffuseTexPath != null) {
                                            WWW texloader = new WWW(basepath + m.diffuseTexPath);
                                            yield return texloader;
                                            m.diffuseTex = texloader.texture;
                                    }
                            }
                           
                    }
            }
            Build();
    }

    private void SetGeometryData(string data) {
            //ALTERATION 1:- Major Problems with different OBJ file format whitespace, this helped:-.
            data = data.Replace("\r\n","\n");
           
            string[] lines = data.Split("\n".ToCharArray()); //
            for(int i = 0; i < lines.Length; i++) {
                    string l = lines[i];
                    if(l.IndexOf("#") != -1) l = l.Substring(0, l.IndexOf("#"));
                   
                    //ALTERATION 1:- whitespace leading to null values, this helped:-
                    l=Regex.Replace(l,@"\s+"," ");
                    l=l.Trim();
                   
                    string[] p = l.Split(" ".ToCharArray());  

                    switch(p[0]) {
                            case O:
                                    buffer.PushObject(p[1].Trim());
                                    break;
                            case G:
                                    buffer.PushGroup(p[1].Trim());
                                    break;
                            case V:        
                                    if (p.Length>=3) {
                                            buffer.PushVertex( new Vector3( cf(p[1]), cf(p[2]), cf(p[3]) ) );
                                    }
                                    break;
                            case VT:
                                    if (p.Length>=2) {
                                            buffer.PushUV(new Vector2( cf(p[1]), cf(p[2]) ));
                                    }
                                    break;
                            case VN:
                                    if (p.Length>=3) {
                                            buffer.PushNormal(new Vector3( cf(p[1]), cf(p[2]), cf(p[3]) ));
                                    }
                                    break;
                            case F:
                                    if (p.Length>=4) {
                                            string[] c;
                                            // ALTERATION 2:- Rough Fix to deal with quads and polys there may be better methods
                                            for (int j=0;j<p.Length-3;j++) {        //Amount of Triangles To Make up Face
                                                    FaceIndices fi = new FaceIndices();     //Get first point
                                                            c=p[1].Trim().Split("/".ToCharArray());
                                                            if (c.Length > 0 && c[0] != string.Empty) {fi.vi = ci(c[0])-1;}
                                                            if (c.Length > 1 && c[1] != string.Empty) {fi.vu = ci(c[1])-1;}
                                                            if (c.Length > 2 && c[2] != string.Empty) {fi.vn = ci(c[2])-1;}
                                                    buffer.PushFace(fi);
                                                    for (int k=0;k<2;k++) {                        
                                                            fi = new FaceIndices(); //Get second and third points (depending on p length)
                                                                    int no=2+k+j;
                                                                    c=p[no].Trim().Split("/".ToCharArray());
                                                                    if (c.Length > 0 && c[0] != string.Empty) {fi.vi = ci(c[0])-1;}
                                                                    if (c.Length > 1 && c[1] != string.Empty) {fi.vu = ci(c[1])-1;}
                                                                    if (c.Length > 2 && c[2] != string.Empty) {fi.vn = ci(c[2])-1;}
                                                            buffer.PushFace(fi);
                                                    }
                                            }
                                    }
                                    break;
                            case MTL:
                                    mtllib = p[1].Trim();
                                    break;
                            case UML:
                                    buffer.PushMaterialName(p[1].Trim());
                                    break;
                    }
            }
            buffer.Trace();
    }
   
    private float cf(string v) {
                    return Convert.ToSingle(v.Trim(), new CultureInfo("en-US"));
    }
   
    private int ci(string v) {
                    return Convert.ToInt32(v.Trim(), new CultureInfo("en-US"));
    }
   
    private bool hasMaterials {
            get {
                    return mtllib != null;
            }
    }
   
    /* ############## MATERIALS */
    private List<MaterialData> materialData;
    private class MaterialData {
            public string name;
            public Color ambient;
            public Color diffuse;
            public Color specular;
            public float shininess;
            public float alpha;
            public int illumType;
            public string diffuseTexPath;
            public Texture2D diffuseTex;
    }
   
    private void SetMaterialData(string data) {
            //ALTERATION 1:- Major Problems with different OBJ file format whitespace, this helped:-.
            data = data.Replace("\r\n","\n");
           
            string[] lines = data.Split("\n".ToCharArray());
           
            materialData = new List<MaterialData>();
            MaterialData current = new MaterialData();
           
            for(int i = 0; i < lines.Length; i++) {
                    string l = lines[i];
                   
                    if(l.IndexOf("#") != -1) l = l.Substring(0, l.IndexOf("#"));
                    //ALTERATION 1:- whitespace leading to null values, this helped:-
                    l=Regex.Replace(l,@"\s+"," ");
                    l=l.Trim();
                   
                    string[] p = l.Split(" ".ToCharArray());
                   
                    switch(p[0]) {
                            case NML:
                                    current = new MaterialData();
                                    current.name = p[1].Trim();
                                    materialData.Add(current);
                                    break;
                            case KA:
                                    current.ambient = gc(p);
                                    break;
                            case KD:
                                    current.diffuse = gc(p);
                                    break;
                            case KS:
                                    current.specular = gc(p);
                                    break;
                            case NS:
                                    current.shininess = cf(p[1]) / 1000;
                                    break;
                            case D:
                            case TR:
                                    current.alpha = cf(p[1]);
                                    break;
                            case MAP_KD:
                                    current.diffuseTexPath = p[1].Trim();
                                    break;
                            case ILLUM:
                                    current.illumType = ci(p[1]);
                                    break;
                                   
                    }
            }      
    }
   
    private Material GetMaterial(MaterialData md) {
            Material m;
           
            if(md.illumType == 2) {
                    m =  new Material(Shader.Find("Specular"));
                    m.SetColor("_SpecColor", md.specular);
                    m.SetFloat("_Shininess", md.shininess);
            } else {
                    m =  new Material(Shader.Find("Diffuse"));
            }
           
            m.SetColor("_Color", md.diffuse);
           
            if(md.diffuseTex != null) m.SetTexture("_MainTex", md.diffuseTex);
           
            return m;
    }
   
    private Color gc(string[] p) {
            return new Color( cf(p[1]), cf(p[2]), cf(p[3]) );
    }

    private void Build() {
            Dictionary<string, Material> materials = new Dictionary<string, Material>();
           
            if(hasMaterials) {
                    foreach(MaterialData md in materialData) {
                            materials.Add(md.name, GetMaterial(md));
                    }
            } else {
                    //ALTERATION 3:- No Material library Found error found on a turbosquid .obj I downloaded
                    //Updated GeometryBuffer.PushGroup() adding g.materialName = "default";  as usemtl entry normally folows g group entry in obj file and will overwrite this "default" material name.
                    //This now works for object with no mtl libraries
                    Material m =  new Material(Shader.Find("Diffuse"));
                    m.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 1.0f));
                    materials.Add("default", m);
            }
           
            GameObject[] ms = new GameObject[buffer.numObjects];
           
            if(buffer.numObjects == 1) {
                    gameObject.AddComponent(typeof(MeshFilter));
                    gameObject.AddComponent(typeof(MeshRenderer));
                    ms[0] = gameObject;
            } else if(buffer.numObjects > 1) {
                    for(int i = 0; i < buffer.numObjects; i++) {
                            GameObject go = new GameObject();
                            go.transform.parent = gameObject.transform;
                            go.AddComponent(typeof(MeshFilter));
                            go.AddComponent(typeof(MeshRenderer));
                            ms[i] = go;
                    }
            }
           
            buffer.PopulateMeshes(ms, materials);
    }
}