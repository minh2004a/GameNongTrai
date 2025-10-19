namespace MyGame.Interactions
{
    public interface IDamageable { void TakeHit(int dmg); }  // quái
    public interface IChoppable { void Chop(int power); }    // rìu
    public interface ITillable { void Till(int power); }    // cuốc
    public interface ICuttable { void Cut(int power); }     // liềm
    public interface IDiggable { void Dig(int power); }     // xẻng
}
