﻿using System;

namespace principia {
namespace ksp_plugin_adapter {

internal static class GLLines {
  public enum Style {
    Solid,
    Dashed,
    Faded,
  }

  public static void Draw(Action line_vertices) {
    try {
      UnityEngine.GL.PushMatrix();
      line_material.SetPass(0);
      UnityEngine.GL.LoadPixelMatrix();
      UnityEngine.GL.Begin(UnityEngine.GL.LINES);

      line_vertices();

      UnityEngine.GL.End();
      UnityEngine.GL.PopMatrix();
    } catch (Exception e) {
      Log.Fatal("Exception while drawing lines: " + e);
    }
  }

  public static void AddSegment(Vector3d world_begin, Vector3d world_end) {
    UnityEngine.Vector3 begin = WorldToMapScreen(world_begin);
    UnityEngine.Vector3 end = WorldToMapScreen(world_end);
    if (begin.z > 0 && end.z > 0) {
      UnityEngine.GL.Vertex3(begin.x, begin.y, 0);
      UnityEngine.GL.Vertex3(end.x, end.y, 0);
    }
  }

  public static DisposablePlanetarium NewPlanetarium(IntPtr plugin,
                                                     XYZ sun_world_position) {
    UnityEngine.Camera camera = PlanetariumCamera.Camera;
    UnityEngine.Vector3 opengl_camera_x_in_world =
        camera.cameraToWorldMatrix.MultiplyVector(
            new UnityEngine.Vector3(1, 0, 0));
    UnityEngine.Vector3 opengl_camera_y_in_world =
        camera.cameraToWorldMatrix.MultiplyVector(
            new UnityEngine.Vector3(0, 1, 0));
    UnityEngine.Vector3 opengl_camera_z_in_world =
        camera.cameraToWorldMatrix.MultiplyVector(
            new UnityEngine.Vector3(0, 0, 1));
    UnityEngine.Vector3 camera_position_in_world =
        ScaledSpace.ScaledToLocalSpace(camera.transform.position);

    // For explanations regarding the OpenGL projection matrix, see
    // http://www.songho.ca/opengl/gl_projectionmatrix.html.  The on-centre
    // projection matrix has the form:
    //   n / w                0                0                0
    //     0                n / h              0                0
    //     0                  0        (n + f) / (n - f)  2 f n / (n - f)
    //     0                  0               -1                0
    // where n and f are the near- and far-clipping distances, and w and h
    // are the half-width and half-height of the screen seen in the focal plane.
    // n is also the focal distance, but we prefer to make that distance 1 metre
    // to avoid having to rescale the result.  The only actual effect of n is
    // the clipping distance, and in space, no one can hear you clip.
    double m00 = camera.projectionMatrix[0, 0];
    double m11 = camera.projectionMatrix[1, 1];
    double field_of_view = Math.Atan2(Math.Sqrt(m00 * m00 + m11 * m11),
                                      m00 * m11);
    return plugin.PlanetariumCreate(sun_world_position,
                                    (XYZ)(Vector3d)opengl_camera_x_in_world,
                                    (XYZ)(Vector3d)opengl_camera_y_in_world,
                                    (XYZ)(Vector3d)opengl_camera_z_in_world,
                                    (XYZ)(Vector3d)camera_position_in_world,
                                    focal: 1,
                                    field_of_view,
                                    ScaledSpace.InverseScaleFactor,
                                    2 * Math.PI / (360 * 60) /*1 arc minute*/,
                                    scaled_space_origin: (XYZ)ScaledSpace.
                                        ScaledToLocalSpace(Vector3d.zero));
  }

  private static UnityEngine.Vector3 WorldToMapScreen(Vector3d world) {
    return PlanetariumCamera.Camera.WorldToScreenPoint(
        ScaledSpace.LocalToScaledSpace(world));
  }

  private static UnityEngine.Material line_material_;

  public static UnityEngine.Material line_material {
    get {
      if (line_material_ == null) {
        line_material_ = new UnityEngine.Material(
#if KSP_VERSION_1_12_5
            UnityEngine.Shader.Find("KSP/Particles/Additive"));
#elif KSP_VERSION_1_7_3
            UnityEngine.Shader.Find("Particles/Additive"));
#endif
      }
      return line_material_;
    }
  }
}

}  // namespace ksp_plugin_adapter
}  // namespace principia
