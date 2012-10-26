using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using MBHEngine.IO;
using MBHEngine.Debug;

namespace MBHEngine.Input
{
    /// <summary>
    /// This helper class allows us to wrap multiple input devices into a single group a functions.  
    /// This way we don't need to check for each device seperatly.
    /// </summary>
    public class InputManager
    {
        /// <summary>
        /// Static instance of the class; this is a singleton.
        /// </summary>
        private static InputManager mInstance = null;

        /// <summary>
        /// A list of all the actions we can check for.
        /// </summary>
        public enum InputActions 
        { 
            LA_LEFT = 0, LA_RIGHT, LA_UP, LA_DOWN,
            RA_LEFT, RA_RIGHT, RA_UP, RA_DOWN, 
            DP_LEFT, DP_RIGHT, DP_UP, DP_DOWN,
            A, B, X, Y, 
            L1, L2, L3, R1, R2, 
            START, BACK 
        };
        
        /// <summary>
        /// Maximum number of controllers.
        /// </summary>
        private int MAX_CONTROLLER_COUNT = 4;

        /// <summary>
        /// A key value pair which matches InputActions to keyboard keys.
        /// </summary>
        private Keys[] mKeyboardActionMap;

        /// <summary>
        /// The state of the keyboard last update.
        /// </summary>
        private KeyboardState mPreviousKeyboardState;

        /// <summary>
        /// The state of the gamepad last update.
        /// </summary>
        private GamePadState mPreviousGamePadState;

        /// <summary>
        /// The state of the gamepad at the start of this frame. It seems
        /// to be able to change mid update, so it is inportant to only
        /// retrive it once to avoid mismatched state info.
        /// </summary>
        private GamePadState mCurrentGamePadState;

        /// <summary>
        /// Has the user been locked to a controller yet?
        /// </summary>
        public bool mIsControllerLocked;

        /// <summary>
        /// Which controller are the locked to?
        /// </summary>
        public PlayerIndex mActiveControllerIndex;

        /// <summary>
        /// Constructor.
        /// </summary>
        private InputManager()
        {
            mIsControllerLocked = false;


            //LA_LEFT = 0, LA_RIGHT, LA_UP, LA_DOWN,
            //RA_LEFT, RA_RIGHT, RA_UP, RA_DOWN, 
            //DP_LEFT, DP_RIGHT, DP_UP, DP_DOWN,
            //A, B, X, Y, 
            //L1, L2, L3, R1, R2, 
            //START, BACK 

            mKeyboardActionMap = new Keys[] { 
                                                Keys.Left,
                                                Keys.Right,
                                                Keys.Up,
                                                Keys.Down,
                                                Keys.OemComma, 
                                                Keys.OemQuestion,
                                                Keys.OemSemicolon,
                                                Keys.OemPeriod,
                                                Keys.Left,
                                                Keys.Right,
                                                Keys.Up,
                                                Keys.Down,
                                                Keys.A,
                                                Keys.B,
                                                Keys.X,
                                                Keys.Y,
                                                Keys.LeftControl,
                                                Keys.LeftShift,
                                                Keys.F4,
                                                Keys.RightControl,
                                                Keys.RightShift,
                                                Keys.Enter,
                                                Keys.Escape
                                            };


            System.Diagnostics.Debug.Assert(mKeyboardActionMap.Length == Enum.GetValues(typeof(InputActions)).Length, "Keyboard mapping does not match InputActions.  Have you added new InputActions but not updated the keyboard mapping?");

            mPreviousKeyboardState = Keyboard.GetState();

            // Until we get a proper boot flow, with PRESS START screen, force the user to user controller
            // at index 0.
            if (CommandLineManager.pInstance["CheatGamePadSelection"] != null)
            {
                mIsControllerLocked = true;
                mActiveControllerIndex = PlayerIndex.One;
                mPreviousGamePadState = mCurrentGamePadState = GamePad.GetState(mActiveControllerIndex);
            }
        }

        /// <summary>
        /// This needs to be called at the start of each update.  It gives the InputManager a chance
        /// to store the current state of the controller so that we have a consistant
        /// state for the entire frame.
        /// </summary>
        public void UpdateBegin()
        {
            mCurrentGamePadState = GamePad.GetState(mActiveControllerIndex);
        }

        /// <summary>
        /// This needs to be called at the end of each update.  It gives the InputManager a chance
        /// to store some data about what happened this frame.
        /// </summary>
        public void UpdateEnd()
        {
            mPreviousKeyboardState = Keyboard.GetState();
            if (mIsControllerLocked == true)
            {
                mPreviousGamePadState = mCurrentGamePadState;
            }
        }

        // Same as the regular CheckAction but assumes that input will not be
        // buffered.
        /// <summary>
        /// Same as the regular CheckAction but assumes that input will not bebuffered.
        /// </summary>
        /// <param name="action">Which action to check, as defined in InputManager.InputActions.</param>
        /// <returns>True if that action happened this frame.</returns>
        public bool CheckAction(InputActions action)
        {
            return CheckAction(action, false);
        }

        // This is the main purpose of this class.  It allows us to check
        // for an action rather than a specific device button.
        //
        /// <summary>
        /// This is the main purpose of this class.  It allows us to check for an action rather 
        /// than a specific device button.
        /// </summary>
        /// <param name="action">Which action to check, as defined in InputManager.InputActions.</param>
        /// <param name="buffer">True if input should only be registers once per press (prevent spamming).</param>
        /// <returns>True if that action happened this frame.</returns>
        public bool CheckAction(InputActions action, bool buffer)
        {
#if !XBOX
            // First let's check the keyboard.
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(mKeyboardActionMap[(int)action]))
            {
                // The button has been pressed this frame, but that might not be the end our checks
                // if the user has requested the input to be buffered.
                if(buffer)
                {
                    // Was this key down last frame?
                    if (mPreviousKeyboardState.IsKeyDown(mKeyboardActionMap[(int)action]))
                    {
                        // If it was then it needs to be ignored this frame.
                        return false;
                    }
                }

                // If we make it to here, the button press is valid.
                return true;
            }
#endif // !XBOX

            // Now let's do the gamepad
            if (mIsControllerLocked == false)
            {
                // Need to detect which controller has pressed start
                for (int i = 0; i < MAX_CONTROLLER_COUNT; i++)
                {
                    if (action == InputActions.START &&
                        GamePad.GetState((PlayerIndex)i).Buttons.Start == ButtonState.Pressed)
                    {
                        mIsControllerLocked = true;
                        mActiveControllerIndex = (PlayerIndex)i;
                        mPreviousGamePadState = mCurrentGamePadState = GamePad.GetState(mActiveControllerIndex);
                        return true;
                    }
                }
                return false;
            }
            
            // Dump brute force checks.  Not as simple as keyboard because we
            // need to handle buttons, dpad, and thumbstick.
            //
            switch (action)
            {
                case InputActions.A:
                    {
                        return CheckButtonState(mCurrentGamePadState.Buttons.A, 
                                                mPreviousGamePadState.Buttons.A, 
                                                ButtonState.Pressed, 
                                                buffer);
                    }
                case InputActions.B:
                    {
                        return CheckButtonState(mCurrentGamePadState.Buttons.B,
                                                mPreviousGamePadState.Buttons.B,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.X:
                    {
                        return CheckButtonState(mCurrentGamePadState.Buttons.X,
                                                mPreviousGamePadState.Buttons.X,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.Y:
                    {
                        return CheckButtonState(mCurrentGamePadState.Buttons.Y,
                                                mPreviousGamePadState.Buttons.Y,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.START:
                    {
                        return CheckButtonState(mCurrentGamePadState.Buttons.Start,
                                                mPreviousGamePadState.Buttons.Start,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.BACK:
                    {
                        return CheckButtonState(mCurrentGamePadState.Buttons.Back,
                                                mPreviousGamePadState.Buttons.Back,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.R1:
                    {
                        return CheckButtonState(mCurrentGamePadState.Buttons.RightShoulder,
                                                mPreviousGamePadState.Buttons.RightShoulder,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.L1:
                    {
                        return CheckButtonState(mCurrentGamePadState.Buttons.LeftShoulder,
                                                mPreviousGamePadState.Buttons.LeftShoulder,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.R2:
                    {
                        if (mCurrentGamePadState.Triggers.Right >= 0.1f)
                        {
                            if (!buffer || mPreviousGamePadState.Triggers.Right < 0.1f)
                            {
                                return true;
                            }
                        }
                        
                        return false;
                    }
                case InputActions.L2:
                    {
                        if (mCurrentGamePadState.Triggers.Left >= 0.1f)
                        {
                            if (!buffer || mPreviousGamePadState.Triggers.Left < 0.1f)
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                case InputActions.L3:
                    {
                        return CheckButtonState(mCurrentGamePadState.Buttons.LeftStick,
                                                mPreviousGamePadState.Buttons.LeftStick,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.DP_LEFT:
                    {
                        return CheckButtonState(mCurrentGamePadState.DPad.Left,
                                                mPreviousGamePadState.DPad.Left,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.DP_RIGHT:
                    {
                        return CheckButtonState(mCurrentGamePadState.DPad.Right,
                                                mPreviousGamePadState.DPad.Right,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.DP_UP:
                    {
                        return CheckButtonState(mCurrentGamePadState.DPad.Up,
                                                mPreviousGamePadState.DPad.Up,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.DP_DOWN:
                    {
                        return CheckButtonState(mCurrentGamePadState.DPad.Down,
                                                mPreviousGamePadState.DPad.Down,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.LA_LEFT:
                    {
                        return CheckAnalogState(mCurrentGamePadState.ThumbSticks.Left.X, 
                                                mPreviousGamePadState.ThumbSticks.Left.X,
                                                -0.1f,
                                                buffer) ||
                               CheckButtonState(mCurrentGamePadState.DPad.Left,
                                                mPreviousGamePadState.DPad.Left,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.LA_RIGHT:
                    {
                        return CheckAnalogState(mCurrentGamePadState.ThumbSticks.Left.X,
                                                mPreviousGamePadState.ThumbSticks.Left.X,
                                                0.1f,
                                                buffer) ||
                               CheckButtonState(mCurrentGamePadState.DPad.Right,
                                                mPreviousGamePadState.DPad.Right,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.LA_UP:
                    {
                        return CheckAnalogState(mCurrentGamePadState.ThumbSticks.Left.Y,
                                                mPreviousGamePadState.ThumbSticks.Left.Y,
                                                0.1f,
                                                buffer) ||
                               CheckButtonState(mCurrentGamePadState.DPad.Up,
                                                mPreviousGamePadState.DPad.Up,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.LA_DOWN:
                    {
                        return CheckAnalogState(mCurrentGamePadState.ThumbSticks.Left.Y,
                                                mPreviousGamePadState.ThumbSticks.Left.Y,
                                                -0.1f,
                                                buffer) ||
                               CheckButtonState(mCurrentGamePadState.DPad.Down,
                                                mPreviousGamePadState.DPad.Down,
                                                ButtonState.Pressed,
                                                buffer);
                    }
                case InputActions.RA_LEFT:
                    {
                        return CheckAnalogState(mCurrentGamePadState.ThumbSticks.Right.X,
                                                mPreviousGamePadState.ThumbSticks.Right.X,
                                                -0.1f,
                                                buffer);
                    }
                case InputActions.RA_RIGHT:
                    {
                        return CheckAnalogState(mCurrentGamePadState.ThumbSticks.Right.X,
                                                mPreviousGamePadState.ThumbSticks.Right.X,
                                                0.1f,
                                                buffer);
                    }
                case InputActions.RA_UP:
                    {
                        return CheckAnalogState(mCurrentGamePadState.ThumbSticks.Right.Y,
                                                mPreviousGamePadState.ThumbSticks.Right.Y,
                                                0.1f,
                                                buffer);
                    }
                case InputActions.RA_DOWN:
                    {
                        return CheckAnalogState(mCurrentGamePadState.ThumbSticks.Right.Y,
                                                mPreviousGamePadState.ThumbSticks.Right.Y,
                                                -0.1f,
                                                buffer);
                    }
            };            

            return false;
        }

        /// <summary>
        /// Helper function for determining if a button has been pressed.
        /// </summary>
        /// <param name="currentState">The current state of a particular button.</param>
        /// <param name="previousState">The state that same button was in last frame.</param>
        /// <param name="targetState">Which state we are checking for.</param>
        /// <param name="buffer">True if we require the state to have changed to count.</param>
        /// <returns>True if this target state was achieved.</returns>
        private bool CheckButtonState(ButtonState currentState, ButtonState previousState, ButtonState targetState, bool buffer)
        {
            if (currentState == targetState)
            {
                if (!buffer || previousState != targetState)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Helper function for determining if a alalog has been pressed.
        /// </summary>
        /// <param name="currentState">The current state of an analog stick.</param>
        /// <param name="previousState">That state of that stick last frame.</param>
        /// <param name="deadZone">The amount of dead zone on the stick.  This should also indicate the direction to check against.</param>
        /// <param name="buffer">True if we require the state to have changed to count.</param>
        /// <returns>True if this target state was achieved.</returns>
        private bool CheckAnalogState(float currentState, float previousState, float deadZone, bool buffer)
        {
            if (System.Math.Abs(currentState) >= System.Math.Abs(deadZone) && CheckAnalogDirection(currentState, deadZone))
            {
                if (!buffer || System.Math.Abs(previousState) < System.Math.Abs(deadZone))
                {
                    return true;
                }
            }

            return false;

        }

        /// <summary>
        /// The CheckAnalogState does its calculations based on absolute values, so we need
        /// an additional check to make sure we are actually pressing in the right directions.
        /// </summary>
        /// <param name="currentState">The currrent state of the analog state, as a signed value.</param>
        /// <param name="direction">The direction to check against.</param>
        /// <returns></returns>
        private bool CheckAnalogDirection(float currentState, float direction)
        {
            if (direction < 0)
            {
                return (currentState < 0);
            }

            if( direction > 0 )
            {
                return (currentState > 0 );
            }

            System.Diagnostics.Debug.Assert(false, "Direction must be a non-zero number.");

            return false;
        }

        /// <summary>
        /// Access to the static instance of this class.
        /// </summary>
        public static InputManager pInstance
        {
            get
            {
                // If this is the first time this instance has been
                // accessed, we need to allocate it.
                if (mInstance == null)
                {
                    mInstance = new InputManager();
                }

                return mInstance;
            }
        }

        /// <summary>
        /// The currently locked controller index.
        /// </summary>
        public PlayerIndex pActiveControllerIndex
        {
            get
            {
                System.Diagnostics.Debug.Assert((true == mIsControllerLocked), "Controller is not locked");

                return mActiveControllerIndex;
            }
            set
            {
                mActiveControllerIndex = value;
            }
        }

        /// <summary>
        /// Check if the controller has been locked yet.
        /// </summary>
        public bool pIsControllerLocked
        {
            get
            {
                return mIsControllerLocked;
            }
            set
            {
                mIsControllerLocked = value;
            }
        }

        /// <summary>
        /// Get the state of the currently active gamepad.  This is helpful for getting detailed information
        /// that the Input Manager doesn't make readily availble, however, it does not handle things like
        /// buffered input so need to be considered in context.
        /// </summary>
        public GamePadState pActiveGamePadState
        {
            get
            { 
                return GamePad.GetState(pActiveControllerIndex, GamePadDeadZone.Circular);
            }
        }
    }
}
