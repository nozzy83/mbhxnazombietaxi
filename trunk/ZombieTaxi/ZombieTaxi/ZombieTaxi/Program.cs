using System;

namespace ZombieTaxi
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            // We wrap the entire game loop in an exception handler so that when things go wrong we can 
            // find out what, rather than just getting a "application failed to run" message.
            //try
            {
                using (Game1 game = new Game1(args))
                    game.Run();
            }

            //catch (Exception e)
            //{
            //    System.Windows.Forms.MessageBox.Show(e.ToString());
            //}
        }
    }
#endif
}

