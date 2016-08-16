using Killer.Proto;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Google.Protobuf;

namespace Scene
{
    public class SceneExtractor
    {
        private static int m_box_id;
        private static int m_sphere_id;
        private static int m_capsule_id;
        private static int m_mesh_id;

        public SceneExtractor()
        {
            m_box_id = 0;
            m_sphere_id = 0;
            m_capsule_id = 0;
            m_mesh_id = 0;
        }

        [MenuItem("Tools/SceneExtractor/ExtractColliders")]
        public static void ExtractorColliders()
        {
            //var path = EditorUtility.SaveFilePanel("Save Scene Collider to...", "E:\\KillerProject_Cooperation_Mode\\KillerProject\\Assets\\Scenes\\Editor\\SceneCollider", "scene");
            var path = EditorUtility.SaveFilePanel("Save Scene Collider to..", "", "", "scene");
            if (path.Length == 0)
                return;

            Debug.Log(path); // + "/" + Application.loadedLevelName
            var f_scene_out = File.Create(path);

            Collider[] allColliders = Resources.FindObjectsOfTypeAll<Collider>();
            Debug.Log(allColliders.Length);

            U3DPhysxScene scene = new U3DPhysxScene();
            scene.SceneName = Application.loadedLevelName;

            for (int i = 0; i < allColliders.Length; ++i)
            {
                Collider col = allColliders[i];

                BoxCollider box_col = col as BoxCollider;
                SphereCollider sphere_col = col as SphereCollider;
                CapsuleCollider capsule_col = col as CapsuleCollider;
                MeshCollider mesh_col = col as MeshCollider;

                if (box_col != null)
                {
                    m_box_id++;
                    scene.BoxCollider.Add(GeneratePhysxBoxCollider(m_box_id, box_col));
                }
                else if (sphere_col != null)
                {
                    m_sphere_id++;
                    scene.SphereCollider.Add(GeneratePhysxSphereCollider(m_sphere_id, sphere_col));
                }
                else if (capsule_col != null)
                {
                    m_capsule_id++;
                    scene.CapsuleCollider.Add(GeneratePhysxCapsuleCollider(m_capsule_id, capsule_col));
                }
                else if (mesh_col != null)
                {
                    m_mesh_id++;
                    scene.MeshCollider.Add(GeneratePhysxConvexMeshCollider(m_mesh_id, mesh_col));
                }
            }

            scene.WriteTo(f_scene_out);
            f_scene_out.Close();
        }

        [MenuItem("Tools/SceneExtractor/LoadColliders")]
        public static void LoadColliders()
        {
            var path = EditorUtility.OpenFilePanel("Load Colliders From...", "", "scene");
            if (path.Length == 0)
                return;

            U3DPhysxScene s;
            using (var file = File.OpenRead(path))
            {
                s = U3DPhysxScene.Parser.ParseFrom(file);
                Debug.Log("load scene box: " + s.BoxCollider.Count);
                Debug.Log("load scene sphere: " + s.SphereCollider.Count);
                Debug.Log("load scene cap: " + s.CapsuleCollider.Count);
            }
        }

        private static U3DPhysxBox GeneratePhysxBoxCollider(int id, BoxCollider box_col)
        {
            U3DPhysxBox box = new U3DPhysxBox();

            box.Id = id;
            box.Type = ColliderType.Box;

            //world pos
            box.Pos = new Killer.Proto.Vector3();
            box.Pos.X = box_col.transform.position.x;
            box.Pos.Y = box_col.transform.position.y;
            box.Pos.Z = box_col.transform.position.z;

            //scale, BUG:考虑父节点对子节点的缩放
            box.XExtents = box_col.size.x * box_col.transform.localScale.x;
            box.YExtents = box_col.size.y * box_col.transform.localScale.y;
            box.ZExtents = box_col.size.z * box_col.transform.localScale.z;

            //rotation considered
            box.Rotation = new Killer.Proto.Vector4();
            box.Rotation.X = box_col.transform.rotation.x;
            box.Rotation.Y = box_col.transform.rotation.y;
            box.Rotation.Z = box_col.transform.rotation.z;
            box.Rotation.W = box_col.transform.rotation.w;

            Debug.Log("BOX ID: " + box.Id);
            Debug.Log("BOX position x: " + box.Pos.X);
            Debug.Log("BOX position y: " + box.Pos.Y);
            Debug.Log("BOX position z: " + box.Pos.Z);

            return box;
        }

        private static U3DPhysxSphere GeneratePhysxSphereCollider(int id, SphereCollider sphere_col)
        {
            U3DPhysxSphere sphere = new U3DPhysxSphere();
            sphere.Id = id;
            sphere.Type = ColliderType.Sphere;

            sphere.Pos = new Killer.Proto.Vector3();
            //world pos
            sphere.Pos.X = sphere_col.transform.position.x;
            sphere.Pos.Y = sphere_col.transform.position.y;
            sphere.Pos.Z = sphere_col.transform.position.z;

            //scale
            double max_scale = Math.Max(Math.Max(sphere_col.transform.localScale.x,
                                                 sphere_col.transform.localScale.y),
                                        sphere_col.transform.localScale.z);
            sphere.Radius = sphere_col.radius * max_scale;

            //rotation considered
            sphere.Rotation = new Killer.Proto.Vector4();
            sphere.Rotation.X = sphere_col.transform.rotation.x;
            sphere.Rotation.Y = sphere_col.transform.rotation.y;
            sphere.Rotation.Z = sphere_col.transform.rotation.z;
            sphere.Rotation.W = sphere_col.transform.rotation.w;


            Debug.Log("Sphere ID: " + sphere.Id);
            Debug.Log("Sphere position x: " + sphere.Pos.X);
            Debug.Log("Sphere position y: " + sphere.Pos.Y);
            Debug.Log("Sphere position z: " + sphere.Pos.Z);

            return sphere;
        }

        private static U3DPhysxCapsule GeneratePhysxCapsuleCollider(int id, CapsuleCollider capsule_col)
        {
            U3DPhysxCapsule cap = new U3DPhysxCapsule();
            cap.Id = id;
            cap.Type = ColliderType.Capsule;

            cap.Pos = new Killer.Proto.Vector3();
            //world pos
            cap.Pos.X = capsule_col.transform.position.x;
            cap.Pos.Y = capsule_col.transform.position.y;
            cap.Pos.Z = capsule_col.transform.position.z;

            //scale, In unity, scale of capsule is a little bit complicated
            //if you scale x/z, the height of the capsule will remain the same, until the threshold
            //if you scale y, the height of the capsule will stretch
            //so currently, we won't consider the scale of capsule
            cap.Height = capsule_col.height; // * capsule_col.transform.localScale.y;
            //double max_scale = Math.Max(capsule_col.transform.localScale.x, capsule_col.transform.localScale.z);
            cap.Raduis = capsule_col.radius; // * max_scale;

            //rotation considered
            cap.Rotation = new Killer.Proto.Vector4();
            cap.Rotation.X = capsule_col.transform.rotation.x;
            cap.Rotation.Y = capsule_col.transform.rotation.y;
            cap.Rotation.Z = capsule_col.transform.rotation.z;
            cap.Rotation.W = capsule_col.transform.rotation.w;

            Debug.Log("CAPSULE ID: " + cap.Id);
            Debug.Log("Capsule position x: " + cap.Pos.X);
            Debug.Log("Capsule position y: " + cap.Pos.Y);
            Debug.Log("Capsule position z: " + cap.Pos.Z);

            return cap;
        }

        private static U3DPhysxMesh GeneratePhysxConvexMeshCollider(int id, MeshCollider mesh_col)
        {
            U3DPhysxMesh mesh = new U3DPhysxMesh();
            mesh.Id = id;
            mesh.Type = ColliderType.Mesh;

            mesh.VertexCount = mesh_col.sharedMesh.vertexCount;
            for (int i = 0; i < mesh_col.sharedMesh.vertexCount; ++i)
            {
                Killer.Proto.Vector3 new_vertice = new Killer.Proto.Vector3();
                //consider scale
                new_vertice.X = mesh_col.sharedMesh.vertices[i].x * mesh_col.transform.localScale.x;
                new_vertice.Y = mesh_col.sharedMesh.vertices[i].y * mesh_col.transform.localScale.y;
                new_vertice.Z = mesh_col.sharedMesh.vertices[i].z * mesh_col.transform.localScale.z;

                mesh.Vertices.Add(new_vertice);
            }

            //BUG:这里还差一个rotate

            Debug.Log("Mesh ID: " + mesh.Id);
            Debug.Log("Mesh Vertex num: " + mesh.VertexCount);

            return mesh;
        }
    }
}