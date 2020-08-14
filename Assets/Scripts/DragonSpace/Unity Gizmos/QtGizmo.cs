namespace DragonSpace.Quadtrees
{
    using UnityEngine;

    public class QtGizmo : IQtVisitor
    {
        public static QtGizmo Draw { get { return new QtGizmo(); } }

        #region Generic interface implementation
        //called by generic normal quadtree
        public void Branch(int node, int depth, int mx, int my, int sx, int sy)
        {
            Gizmos.color = new Color(0, 0, 1, 0.5f);
            Gizmos.DrawWireCube(
                new Vector3(mx, -depth * 6, my),
                new Vector3(sx * 2, 0.5f, sy * 2));
        }

        //called by generic normal quadtree
        public void Leaf(int count, int node, int depth, int mx, int my, int sx, int sy)
        {
            Vector3 center = new Vector3(mx, -depth * 6, my);
            Vector3 size = new Vector3(sx * 2f, 0.5f, sy * 2f);
            if (count > 0)
            {
                Gizmos.color = new Color(1, 0, 1, 0.8f);
                Gizmos.DrawCube(center, size);
            }
        }

        //called by generic loose quadtree
        public void Branch(int lft, int top, int rgt, int btm)
        {
            float mx = lft + ((rgt - lft) / 2);
            float my = btm + ((top - btm) / 2);

            Gizmos.color = Color.yellow;
            
            Gizmos.DrawWireCube(
                new Vector3(mx, -1, my),
                new Vector3((rgt - lft), 0.5f, (top - btm)));
        }

        //called by generic loose quadtree
        public void Leaf(int count, int lft, int top, int rgt, int btm)
        {
            float mx = lft + ((rgt - lft) / 2);
            float my = btm + ((top - btm) / 2);

            Gizmos.color = Color.cyan;
            
            Gizmos.DrawCube(
                new Vector3(mx, -1, my),
                new Vector3((rgt - lft), 0.5f, (top - btm)));

            //Gizmos.color = Color.cyan;
            //Gizmos.DrawLine(
            //    new Vector3(lft, 0, btm),
            //    new Vector3(lft, 0, top));
            //Gizmos.DrawLine(
            //    new Vector3(lft, 0, top),
            //    new Vector3(rgt, 0, top));
            //Gizmos.DrawLine(
            //    new Vector3(rgt, 0, btm),
            //    new Vector3(rgt, 0, top));
            //Gizmos.DrawLine(
            //    new Vector3(lft, 0, btm),
            //    new Vector3(rgt, 0, btm));
        }
        #endregion
    }
}
