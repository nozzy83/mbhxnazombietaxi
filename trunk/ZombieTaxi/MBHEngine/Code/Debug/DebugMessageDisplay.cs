using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;

namespace MBHEngine.Debug
{
    /// <summary>
    /// Singleton class used for displaying debug information to the screen.  This
    /// is helpful for analyzing things in fullscreen mode, as well as looking at
    /// data that changes frequenty.
    /// </summary>
    /// <remarks>
    /// This class, by its very nature, encures a heavy amount of garabage.  To actually
    /// use the DebugMessageDisplay in any significant way, ALLOW_GARBAGE must be defined.
    /// </remarks>
    public class DebugMessageDisplay
    {
        /// <summary>
        /// Class used to wrap a single debug message and its associated information.
        /// </summary>
        private class DebugMessage
        {
            /// <summary>
            /// The message to display.
            /// </summary>
            public String mMessage = null;

            /// <summary>
            /// The tag used to filter this message.
            /// </summary>
            public String mTag = null;
        };

        /// <summary>
        /// The static instance of this class, making this a singleton.
        /// </summary>
        static private DebugMessageDisplay mInstance = null;

        /// <summary>
        /// This array will store our messages that are only shown for one frame.
        /// </summary>
        private List<DebugMessage> mDynamicMsgs;

        /// <summary>
        /// This array will store the constant strings.
        /// </summary>
        private DebugMessage[] mConstantMsgs;

        /// <summary>
        /// This is how many constant strings we are going to store.
        /// </summary>
        private int mMaxConstMsgs = 10;

#if ALLOW_GARBAGE
        /// <summary>
        /// This will keep track of the current message number.
        /// </summary>
        private int mCurMsgNum = 0;
#endif // ALLOW_GARBAGE

        /// <summary>
        /// The font object we use for rendering.
        /// </summary>
        private SpriteFont mFont;

        /// <summary>
        /// Offset for keeping the debug display within the safe zone of a TV.
        /// </summary>
        private int safeZoneOffsetX = 40;
        private int safeZoneOffsetY = 20;

        /// <summary>
        /// The color of the drop shadow that falls behind text entries.
        /// </summary>
        private Color mTextShadowColor;

        /// <summary>
        /// The color used to render the text entries.
        /// </summary>
        private Color mTextColor;

        /// <summary>
        /// The name of the layer currently being shown.
        /// </summary>
        private String mCurrentTag;

        /// <summary>
        /// The message that appears at the top of the debug display.
        /// </summary>
        private DebugMessage mDefaultHeader;

        /// <summary>
        /// Constructor.
        /// </summary>
        private DebugMessageDisplay()
        {
            // Create the font
            mFont = GameObjectManager.pInstance.pContentManager.Load<SpriteFont>("Fonts\\DebugDisplay");

            mDynamicMsgs = new List<DebugMessage>(32);

            mDefaultHeader = new DebugMessage();
            mDefaultHeader.mMessage = "Debug Information:";
            mDynamicMsgs.Add(mDefaultHeader);

            // Allocate our constant string
            mConstantMsgs = new DebugMessage[mMaxConstMsgs];

            // Clear all the messages.
            for (int i = 0; i < mMaxConstMsgs; i++)
            {
                DebugMessage temp = new DebugMessage();
                temp.mMessage = "";
                mConstantMsgs[i] = temp;
            }

            mTextShadowColor = Color.Black;
            mTextColor = Color.White;

            mCurrentTag = null;
        }

        /// <summary>
        /// Renders the dynamic messages.
        /// </summary>
        /// <param name="spriteBatch">Sprite batch used for rendering.</param>
        private void RenderDynamicMsgs(SpriteBatch spriteBatch)
        {
            Int32 spacing = mFont.LineSpacing;

            Int32 count = 0;

            for (Int16 i = 0; i < mDynamicMsgs.Count; i++)
            {
                if (null != mDynamicMsgs[i].mMessage && mCurrentTag == mDynamicMsgs[i].mTag)
                {
                    Single Y = (spacing * count) + safeZoneOffsetY;
                    count++;

                    // The dynamic message is just stored as a long single string, so we only need one
                    // draw call (as well as a copy for the drop shadow).
                    spriteBatch.DrawString(mFont, mDynamicMsgs[i].mMessage, new Vector2(1 + safeZoneOffsetX, 1 + Y), mTextShadowColor);
                    spriteBatch.DrawString(mFont, mDynamicMsgs[i].mMessage, new Vector2(0 + safeZoneOffsetX, 0 + Y), mTextColor);
                }
            }
        }

        /// <summary>
        /// Renders the constant messages.
        /// </summary>
        /// <param name="spriteBatch">Sprite batch used for rendering.</param>
        private void RenderConstMsgs(SpriteBatch spriteBatch)
        {
            // Determine the starting alpha value
            float alphaCur = 1.0f;
            float alphaDec = 1.0f / mMaxConstMsgs;

            // Loop through all the messages
            for (int i = 0; i < mMaxConstMsgs; i++)
            {
                if (mConstantMsgs[i].mMessage != null)
                {
                    // Get the bottom of the window.
                    int bottom = GameObjectManager.pInstance.pGraphicsDevice.Viewport.Height;

                    // The font is anchored at the top so we need to move it up by it's height in order
                    // to get it on screen.  We add an extra pixel so that the drop shadow fits on screen
                    // too.
                    bottom -= 15 + 1;

                    // We want each entry in the array to appear above the previous entry.
                    bottom -= i * 15;

                    // Draw the message at the bottom of the screen.
                    spriteBatch.DrawString(mFont, mConstantMsgs[i].mMessage, new Vector2(1 + safeZoneOffsetX, bottom + 1 - safeZoneOffsetY), mTextShadowColor * alphaCur);
                    spriteBatch.DrawString(mFont, mConstantMsgs[i].mMessage, new Vector2(0 + safeZoneOffsetX, bottom - safeZoneOffsetY), mTextColor * alphaCur);

                    // Make it so the next message is a little less visable
                    alphaCur -= alphaDec;
                }
            }
        }

        /// <summary>
        /// Renders all the current debug messages.  Should be called during the Draw phase of the game loop.
        /// </summary>
        /// <remarks>
        /// This function does nothing outside of DEBUG.
        /// </remarks>
        /// <param name="spriteBatch">The sprite batch to render to.</param>
        public void Render(SpriteBatch spriteBatch)
        {
#if ALLOW_GARBAGE
            RenderDynamicMsgs(spriteBatch);

            // Draw the constant messages
            RenderConstMsgs(spriteBatch);
#endif
        }

        /// <summary>
        /// Clears all the dyanmic messages.  Should be called at the start of every update.
        /// This is so that if there renderer is not called at the same frequency as the 
        /// update, we won't get duplicate messages.
        /// </summary>
        public void ClearDynamicMessages()
        {
            // When we finish rendering we want to clear the dynamic messages
            mDynamicMsgs.Clear();

            // Add the default header back to the top.
            mDynamicMsgs.Add(mDefaultHeader);
        }

        
#if ALLOW_GARBAGE
        /// <summary>
        /// Interface for adding dynamic messages to the system.  These get cleared every frame
        /// so they need to be re-added every frame.  This is perfect for data that changes constantly,
        /// such as a frame rate counter.
        /// </summary>
        /// <param name="newMsg">The message to display.</param>
        public void AddDynamicMessage(String newMsg, String tag = null)
        {
            // TODO: This should probably just keep reusing the same entries rather than reallocating
            //       every time. Not too worries since this is all wrapped in ALLOW_GARBAGE.
            DebugMessage d = new DebugMessage();
            d.mMessage = newMsg;
            d.mTag = tag;

            mDynamicMsgs.Add(d);
        }
#endif

#if ALLOW_GARBAGE
        /// <summary>
        /// Interface for adding constant messages to the system.  These never get cleared, they are
        /// just concatinated to the end of the list.  This is pefect for data that you want to keep a
        /// record of events on, such as objects being added and removed from the game.
        /// </summary>
        /// <param name="newMsg">The message to display.</param>
        public void AddConstantMessage(String newMsg)
        {
            // Start at the last message and copy in the next one
            for (int i = mMaxConstMsgs - 1; i > 0; i--)
            {
                // Copy the message below into the message above
                mConstantMsgs[i].mMessage = mConstantMsgs[i - 1].mMessage;
                mConstantMsgs[i].mTag = mConstantMsgs[i - 1].mTag;
            }

            // And finally copy in the new message
            mCurMsgNum++;

            mConstantMsgs[0].mMessage = mCurMsgNum + ": " + newMsg;
            mConstantMsgs[0].mTag = null;
        }
#endif

        /// <summary>
        /// Access to single static instance of the class.  This is the interface for our singleton.
        /// </summary>
        public static DebugMessageDisplay pInstance
        {
            get
            {
                // If this is the first time this is called, instantiate our
                // static instance of the class.
                if (mInstance == null)
                {
                    mInstance = new DebugMessageDisplay();
                }

                // Either way, at this point we should have an instantiated version
                // if the class.
                return mInstance;
            }
        }

        /// <summary>
        /// Sets the current layer of debug being rendered.
        /// Set to null to return to default.
        /// </summary>
        public String pCurrentTag
        {
            get
            {
                return mCurrentTag;
            }

            set
            {
                mCurrentTag = value;
            }
        }
    }
}
