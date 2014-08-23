using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Henge3D.Physics;
using Henge3D.Pipeline;

namespace PhysicsServer {
  public class Body : RigidBody {
    private Model _model;
    private string _tag;
    private int _player;

    public Body(Game game, Model model, string tag, int player = -1)
      : base((RigidBodyModel)model.Tag) {
      _model = model;
      _tag = tag;
      _player = player;
    }

    public int Player() {
      return _player;
    }

    public string Tag() {
      return _tag;
    }

    public string Matrix(bool add) {
      return new StringBuilder().Append(Transform.Combined.M11).Append(",").Append(Transform.Combined.M12).Append(",").Append(Transform.Combined.M13).Append(",").Append(Transform.Combined.M14).Append(",")
        .Append(Transform.Combined.M21).Append(",").Append(Transform.Combined.M22).Append(",").Append(Transform.Combined.M23).Append(",").Append(Transform.Combined.M24).Append(",")
        .Append(Transform.Combined.M31).Append(",").Append(Transform.Combined.M32).Append(",").Append(Transform.Combined.M33).Append(",").Append(Transform.Combined.M34).Append(",")
        .Append(Transform.Combined.M41).Append(",").Append(Transform.Combined.M42).Append(",").Append(Transform.Combined.M43).Append(",").Append(Transform.Combined.M44).ToString();
    }
  }
}
