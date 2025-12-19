using UnityEngine;
using SoftwareRenderer.Core;

namespace SoftwareRenderer.Scene
{
    /// <summary>
    /// 相机类
    /// </summary>
    public class Camera
    {
        private float fov = 60f;
        private float aspect = 1f;
        private float near = 0.1f;
        private float far = 100f;
        private bool reverseZ = false;

        private Vector3 eye = Vector3.zero;
        private Vector3 center = Vector3.forward;
        private Vector3 up = Vector3.up;

        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projectionMatrix;
        private bool dirty = true;

        public float Fov
        {
            get => fov;
            set { fov = value; dirty = true; }
        }

        public float Aspect
        {
            get => aspect;
            set { aspect = value; dirty = true; }
        }

        public float Near
        {
            get => near;
            set { near = value; dirty = true; }
        }

        public float Far
        {
            get => far;
            set { far = value; dirty = true; }
        }

        public Vector3 Eye
        {
            get => eye;
            set { eye = value; dirty = true; }
        }

        public Vector3 Center
        {
            get => center;
            set { center = value; dirty = true; }
        }

        public Vector3 Up
        {
            get => up;
            set { up = value; dirty = true; }
        }

        public void SetPerspective(float fov, float aspect, float near, float far)
        {
            this.fov = fov;
            this.aspect = aspect;
            this.near = near;
            this.far = far;
            dirty = true;
        }

        public void LookAt(Vector3 eye, Vector3 center, Vector3 up)
        {
            this.eye = eye;
            this.center = center;
            this.up = up;
            dirty = true;
        }

        public Matrix4x4 GetViewMatrix()
        {
            if (dirty) Update();
            return _viewMatrix;
        }

        public Matrix4x4 GetProjectionMatrix()
        {
            if (dirty) Update();
            return _projectionMatrix;
        }

        public Matrix4x4 GetViewProjectionMatrix()
        {
            return GetProjectionMatrix() * GetViewMatrix();
        }

        private void Update()
        {
            _viewMatrix = Core.Math.MathUtils.LookAt(eye, center, up);
            _projectionMatrix = Core.Math.MathUtils.Perspective(fov * Mathf.Deg2Rad, aspect, near, far, reverseZ);
            dirty = false;
        }
    }
}

