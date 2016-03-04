using DigitalRune.Geometry;
using DigitalRune.Geometry.Meshes;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Mathematics.Algebra;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace KinectGame.GeneralComponents.RigidBodyRendered
{
    public class DrawableShape
    {
        private VertexBuffer _vertexBuffer;
        private int _numberOfTriangles;


        // Creates the graphics resources (e.g. the vertex buffer).
        public void LoadContent(GraphicsDevice graphicsDevice, IGeometricObject geometricObject)
        {
            // Create a mesh for the given shape. (The arguments define the desired resolution if the 
            // mesh is only approximated - for example for a sphere.)
            TriangleMesh mesh = geometricObject.Shape.GetMesh(0.01f, 3);

            // Abort if we have nothing to draw. (This happens for "EmptyShapes".)
            if (mesh.Vertices.Count == 0)
                return;

            // Apply the scaling that is defined in the geometric object.
            if (geometricObject.Scale != Vector3F.One)
                mesh.Transform(Matrix44F.CreateScale(geometricObject.Scale));

            _numberOfTriangles = mesh.NumberOfTriangles;

            // Create vertex data for a triangle list.
            VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[mesh.NumberOfTriangles * 3];

            // Create vertex normals. Do not merge normals if the angle between the triangle normals
            // is > 70°.
            Vector3F[] normals = mesh.ComputeNormals(false, MathHelper.ToRadians(70));

            // Loop over all triangles and copy vertices to the vertices array.
            for (int i = 0; i < _numberOfTriangles; i++)
            {
                // Get next triangle of the mesh.
                Triangle triangle = mesh.GetTriangle(i);

                // Add new vertex data.
                // DigitalRune.Geometry uses counter-clockwise front faces. XNA uses
                // clockwise front faces (CullMode.CullCounterClockwiseFace) per default. 
                // Therefore we change the vertex orientation of the triangles. 
                // We could also keep the vertex order and change the CullMode to CullClockwiseFace.
                vertices[i * 3 + 0] = new VertexPositionNormalTexture(
                  (Vector3)triangle.Vertex0,
                  (Vector3)normals[i * 3 + 0],
                  Vector2.Zero);
                vertices[i * 3 + 1] = new VertexPositionNormalTexture(
                  (Vector3)triangle.Vertex2,  // triangle.Vertex2 instead of triangle.Vertex1 to change vertex order!
                  (Vector3)normals[i * 3 + 2],
                  Vector2.Zero);
                vertices[i * 3 + 2] = new VertexPositionNormalTexture(
                  (Vector3)triangle.Vertex1,  // triangle.Vertex1 instead of triangle.Vertex2 to change vertex order!
                  (Vector3)normals[i * 3 + 1],
                  Vector2.Zero);
            }

            // Create a vertex buffer.
            _vertexBuffer = new VertexBuffer(
              graphicsDevice,
              vertices[0].GetType(),
              vertices.Length,
              BufferUsage.WriteOnly);

            // Fill the vertex buffer.
            _vertexBuffer.SetData(vertices);
        }


        // Draws the triangle mesh.
        public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect, Pose pose, Color color)
        {
            if (_vertexBuffer == null)
                return;

            // Select the vertex buffer.
            graphicsDevice.SetVertexBuffer(_vertexBuffer);

            // The parameter 'pose' defines the world matrix and can be implicitly converted to 
            // a XNA Matrix.
            effect.World = pose;
            effect.DiffuseColor = color.ToVector3();

            // Draw the vertex buffer.
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _numberOfTriangles);
            }
        }
    }
}
