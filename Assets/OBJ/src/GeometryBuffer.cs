//OBJ import for Unity3d 2/3
//OBJ.cs by bartek drozdz of http://www.everyday3d.com (ammendments by Jon Martin of jon-martin.com & fusedworks.com)
 
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
 
public class GeometryBuffer {
 
        private List<ObjectData> objects;
        public List<Vector3> vertices;
        public List<Vector2> uvs;
        public List<Vector3> normals;
       
        private ObjectData current;
        private class ObjectData {
                public string name;
                public List<GroupData> groups;
                public List<FaceIndices> allFaces;
                public ObjectData() {
                        groups = new List<GroupData>();
                        allFaces = new List<FaceIndices>();
                }
        }
       
        private GroupData curgr;
        private class GroupData {
                public string name;
                public string materialName;
                public List<FaceIndices> faces;
                public GroupData() {
                        faces = new List<FaceIndices>();
                }
                public bool isEmpty { get { return faces.Count == 0; } }
        }
       
        public GeometryBuffer() {
                objects = new List<ObjectData>();
                ObjectData d = new ObjectData();
                d.name = "default";
                objects.Add(d);
                current = d;
               
                GroupData g = new GroupData();
                g.name = "default";
                // OBJ.cs ALTERATION3: added to help with missing material libraries see
                g.materialName = "default";    
                d.groups.Add(g);
                curgr = g;
               
                vertices = new List<Vector3>();
                uvs = new List<Vector2>();
                normals = new List<Vector3>();
        }
       
        public void PushObject(string name) {
                //Debug.Log("Adding new object " + name + ". Current is empty: " + isEmpty);
                if(isEmpty) objects.Remove(current);
               
                ObjectData n = new ObjectData();
                n.name = name;
                objects.Add(n);
               
                GroupData g = new GroupData();
                g.name = "default";
                n.groups.Add(g);
               
                curgr = g;
                current = n;
        }
       
        public void PushGroup(string name) {
                if(curgr.isEmpty) current.groups.Remove(curgr);
                GroupData g = new GroupData();
                g.name = name;
                // OBJ.cs ALTERATION3: added to help with missing material libraries see
                g.materialName = "default";    
                current.groups.Add(g);
                curgr = g;
        }
       
        public void PushMaterialName(string name) {
                Debug.Log("Pushing new material " + name + " with curgr.empty=" + curgr.isEmpty);
                if(!curgr.isEmpty) PushGroup(name);
                if(curgr.name == "default") curgr.name = name;
                curgr.materialName = name;
        }
       
        //OBJ.cs ALTERATION 4:- Lets say they moved the material library retrieve material names that need to be set up.
        public string[] ReturnMaterialNames() {
                string[] EmptyMaterials;
                int i=0;
                foreach(ObjectData od in objects) {
                        i+=od.groups.Count;
                }
                EmptyMaterials = new string[i];
                i=0;
                foreach(ObjectData od in objects) {
                        foreach(GroupData gd in od.groups) {
                                EmptyMaterials[i]=gd.materialName;
                        }
                }              
                return EmptyMaterials;
        }
       
       
        public void PushVertex(Vector3 v) {
                vertices.Add(v);
        }
       
        public void PushUV(Vector2 v) {
                uvs.Add(v);
        }
       
        public void PushNormal(Vector3 v) {
                normals.Add(v);
        }
       
        public void PushFace(FaceIndices f) {
                curgr.faces.Add(f);
                current.allFaces.Add(f);
        }
       
        public void Trace() {
                Debug.Log("OBJ has " + objects.Count + " object(s)");
                Debug.Log("OBJ has " + vertices.Count + " vertice(s)");
                Debug.Log("OBJ has " + uvs.Count + " uv(s)");
                Debug.Log("OBJ has " + normals.Count + " normal(s)");
                foreach(ObjectData od in objects) {
                        Debug.Log(od.name + " has " + od.groups.Count + " group(s)");
                        foreach(GroupData gd in od.groups) {
                                Debug.Log(od.name + "/" + gd.name + " has " + gd.faces.Count + " faces(s)");
                        }
                }
               
        }
       
        public int numObjects { get { return objects.Count; } }
        public bool isEmpty { get { return vertices.Count == 0; } }
        public bool hasUVs { get { return uvs.Count > 0; } }
        public bool hasNormals { get { return normals.Count > 0; } }
       
        public void PopulateMeshes(GameObject[] gs, Dictionary<string, Material> mats) {
                if(gs.Length != numObjects) {
                                return; // Should not happen unless obj file is corrupt...
                        }
                for(int i = 0; i < gs.Length; i++) {
                        ObjectData od = objects[i];
                       
                        if(od.name != "default") gs[i].name = od.name;
                       
                        Vector3[] tvertices = new Vector3[od.allFaces.Count];
                        Vector2[] tuvs = new Vector2[od.allFaces.Count];
                        Vector3[] tnormals = new Vector3[od.allFaces.Count];
               
                        int k = 0;
                        foreach(FaceIndices fi in od.allFaces) {
                                tvertices[k] = vertices[fi.vi];
                                if(hasUVs) tuvs[k] = uvs[fi.vu];
                                if(hasNormals) tnormals[k] = normals[fi.vn];
                                k++;
                        }
               
                        Mesh m = (gs[i].GetComponent(typeof(MeshFilter)) as MeshFilter).mesh;
                        m.vertices = tvertices;
                        if(hasUVs) m.uv = tuvs;
                        if(hasNormals) m.normals = tnormals;
                       
                        if(od.groups.Count == 1) {
                                GroupData gd = od.groups[0];
                               
                                gs[i].renderer.material = mats[gd.materialName];
                               
                                //Alteration 6 ADDED TO NAME MATERIALS
                                gs[i].renderer.material.name = gd.materialName;
                               
                                int[] triangles = new int[gd.faces.Count];
                                for(int j = 0; j < triangles.Length; j++) triangles[j] = j;
                               
                                m.triangles = triangles;
                               
                                //Alteration 5 Added for those OBJ file without normals
                                if(!hasNormals) {
                                        m.RecalculateNormals();
                                }
                                CalculateTangents(m);
                               
                        } else {
                                int gl = od.groups.Count;
                                Material[] sml = new Material[gl];
                                m.subMeshCount = gl;
                                int c = 0;
                               
                                for(int j = 0; j < gl; j++) {
                                        sml[j] = mats[od.groups[j].materialName];
                                       
                                        //Alteration 6 ADDED TO NAME MATERIALS
                                        sml[j].name = od.groups[j].materialName;
                                       
                                        int[] triangles = new int[od.groups[j].faces.Count];
                                        int l = od.groups[j].faces.Count + c;
                                        int s = 0;
                                        for(; c < l; c++, s++) triangles[s] = c;
                                        m.SetTriangles(triangles, j);
                                        //Alteration 5 Added for those OBJ file without normals
                                        if(!hasNormals) {
                                                m.RecalculateNormals();
                                        }
                                        CalculateTangents(m);
                                }
                                gs[i].renderer.materials = sml;
                        }
                }
        }
 
        //Alteration 7 Added CalculateTangents Function to cater for bumpmaps on the mesh etc.
        public void CalculateTangents(Mesh mesh) {
                int triangleCount = mesh.triangles.Length / 3;
                int vertexCount = mesh.vertices.Length;
 
                Vector3[] tan1 = new Vector3[vertexCount];
                Vector3[] tan2 = new Vector3[vertexCount];
                Vector4[] tangents = new Vector4[vertexCount];
 
                for(long a = 0; a < triangleCount; a+=3) {
                        long i1 = mesh.triangles[a+0];
                        long i2 = mesh.triangles[a+1];
                        long i3 = mesh.triangles[a+2];
                        Vector3 v1 = mesh.vertices[i1];
                        Vector3 v2 = mesh.vertices[i2];
                        Vector3 v3 = mesh.vertices[i3];
                        Vector2 w1 = mesh.uv[i1];
                        Vector2 w2 = mesh.uv[i2];
                        Vector2 w3 = mesh.uv[i3];
 
                        float x1 = v2.x - v1.x;
                        float x2 = v3.x - v1.x;
                        float y1 = v2.y - v1.y;
                        float y2 = v3.y - v1.y;
                        float z1 = v2.z - v1.z;
                        float z2 = v3.z - v1.z;
 
                        float s1 = w2.x - w1.x;
                        float s2 = w3.x - w1.x;
                        float t1 = w2.y - w1.y;
                        float t2 = w3.y - w1.y;
 
                        float r = 1.0f / (s1 * t2 - s2 * t1);
 
                        Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                        Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);
 
                        tan1[i1] += sdir;
                        tan1[i2] += sdir;
                        tan1[i3] += sdir;
 
                        tan2[i1] += tdir;
                        tan2[i2] += tdir;
                        tan2[i3] += tdir;
                }
 
                for (long a = 0; a < vertexCount; ++a)  {
                        Vector3 n = mesh.normals[a];
                        Vector3 t = tan1[a];
                        Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
                        tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
                        tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
                }
                mesh.tangents = tangents;
        }
       
}