namespace Mixer.Audio
{
    public interface IAudioProvider
    {
        #region Methods

        void Push(byte[] chunk);

        int Pull(out byte[] chunk);

        int Peak(out byte[] chunk);

        #endregion Methods
    }
}
