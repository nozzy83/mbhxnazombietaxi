using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngine.Input;
using MBHEngineContentDefs;
using ZombieTaxiContentDefs;
using ZombieTaxi.StatBoost.Behaviours;

namespace ZombieTaxi.Behaviours
{
    /// <summary>
    /// Popup shown to the user when they want to assign a task to a Stranded who has been rescued.
    /// </summary>
    class StrandedPopup : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// A button the the popup. Consists of an image, with some text that is showned when this button
        /// is highlighted.
        /// </summary>
        private class Button
        {
            /// <summary>
            /// The icon displayed for this button.
            /// </summary>
            private GameObject mObject;

            /// <summary>
            /// An image rendered under the icon.
            /// </summary>
            private GameObject mBG;

            /// <summary>
            /// The button which will be transitioned to if the user pressed "Left" on the controller.
            /// </summary>
            private Button mLeft;

            /// <summary>
            /// The Button which will be transitioned to if the user presses "Right" on the controller.
            /// </summary>
            private Button mRight;
            
            /// <summary>
            /// A short piece of text to be displayed when this Button is currently highlighted.
            /// </summary>
            private String mHintText;

            /// <summary>
            /// The a button is actually pressed, a message is broadcast, and in the message is an enum
            /// indicating what button was pressed. This is where that enum is stored for this Button.
            /// </summary>
            private StrandedPopupDefinition.ButtonTypes mType;

            /// <summary>
            /// Prealloced messages to avoid GC.
            /// </summary>
            private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="def">Defines how this Button should be initialized.</param>
            public Button(StrandedPopupDefinition.ButtonDefinition def)
            {
                // Create the icon to show on the button.
                mObject = new GameObject(def.mIconFileName);

                // All buttons use the same background image for now.
                mBG = new GameObject("GameObjects\\Interface\\StrandedPopup\\IconBG\\IconBG");
                mBG.pPosition = mObject.pPosition;

                // By default a Button has no siblings.
                mLeft = mRight = null;

                mType = def.mButtonType;

                mHintText = def.mHintText;

                mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            }

            /// <summary>
            /// Called when the parent popup is added to the GameObjectManager.
            /// </summary>
            public void OnAdd()
            {
                GameObjectManager.pInstance.Add(mObject);
                GameObjectManager.pInstance.Add(mBG);
            }

            /// <summary>
            /// Called when the parent popup is added to the GameObjectManager.
            /// </summary>
            public void OnRemove()
            {
                GameObjectManager.pInstance.Remove(mObject);
                GameObjectManager.pInstance.Remove(mBG);
            }

            /// <summary>
            /// Called when the Button is highlighted.
            /// </summary>
            public void OnRollOver()
            {
                mSetActiveAnimationMsg.mAnimationSetName_In = "RollOver";
                mObject.OnMessage(mSetActiveAnimationMsg);
            }

            /// <summary>
            /// Called when this Button was highlighted, but the cursor has moved to another
            /// Button.
            /// </summary>
            public void OnRollOff()
            {
                mSetActiveAnimationMsg.mAnimationSetName_In = "None";
                mObject.OnMessage(mSetActiveAnimationMsg);
            }

            /// <summary>
            /// Helper fuction for linking this button with another. Sets this Button's pLeft, but also
            /// set's pLeft's pRight to this Button.
            /// </summary>
            /// <param name="leftButton"></param>
            public void LinkLeft(Button leftButton)
            {
                mLeft = leftButton;
                leftButton.pRight = this;
            }

            /// <summary>
            /// Access to the icon used by this Button.
            /// </summary>
            public GameObject pGameObject
            {
                get
                {
                    return mObject;
                }
                set
                {
                    mObject = value;
                }
            }

            /// <summary>
            /// Access to the Button to the left of this button.
            /// </summary>
            public Button pLeft
            {
                get
                {
                    return mLeft;
                }
                set
                {
                    mLeft = value;
                }
            }

            /// <summary>
            /// Access to the Button to the right of this Button.
            /// </summary>
            public Button pRight
            {
                get
                {
                    return mRight;
                }
                set
                {
                    mRight = value;
                }
            }

            /// <summary>
            /// The text to display when this Button is highlighted.
            /// </summary>
            public String pHintText
            {
                get
                {
                    return mHintText;
                }
                set
                {
                    mHintText = value;
                }
            }

            /// <summary>
            /// Information to attach to message when this button is selected.
            /// </summary>
            public StrandedPopupDefinition.ButtonTypes pOnCloseSelection
            {
                get
                {
                    return mType;
                }
                set
                {
                    mType = value;
                }
            }
        }

        /// <summary>
        /// Sent when the Popup is closed. Includes information about which Button was actually selected.
        /// </summary>
        public class OnPopupClosedMessage : BehaviourMessage
        {
            /// <summary>
            /// Indicates which type of Button was pressed.
            /// </summary>
            public StrandedPopupDefinition.ButtonTypes mSelection_In;

            /// <summary>
            /// Call to put this message back into default state.
            /// </summary>
            public override void Reset()
            {
                mSelection_In = StrandedPopupDefinition.ButtonTypes.None;
            }
        }

        /// <summary>
        /// Image used for background window.
        /// </summary>
        private GameObject mWindow;

        /// <summary>
        /// Image used for selection cursor.
        /// </summary>
        private GameObject mCursor;

        /// <summary>
        /// The font object we use for rendering.
        /// </summary>
        private SpriteFont mFont;

        /// <summary>
        /// The currently selected Button.
        /// </summary>
        private Button mCurrentButton;

        /// <summary>
        /// A list of all Buttons on this Popup.
        /// </summary>
        private List<Button> mButtons;

        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        private SpriteRender.SetActiveAnimationMessage mSetActiveAnimationMsg;
        private OnPopupClosedMessage mOnPopupCloseMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public StrandedPopup(GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            base.LoadContent(fileName);

            StrandedPopupDefinition def = GameObjectManager.pInstance.pContentManager.Load<StrandedPopupDefinition>(fileName);

            // All versions of the Stranded popup use the same Window and Cursor.
            mWindow = new GameObject("GameObjects\\Interface\\StrandedPopup\\Window\\Window");
            mCursor = new GameObject("GameObjects\\Interface\\StrandedPopup\\Cursor\\Cursor");

            // Create the font for displaying the hint text.
            mFont = GameObjectManager.pInstance.pContentManager.Load<SpriteFont>("Fonts\\Retro");

            mButtons = new List<Button>(def.mButtons.Count);

            mCurrentButton = null;

            for (Int32 i = 0; i < def.mButtons.Count; i++)
            {
                // Create all the Buttons.
                Button temp = new Button(def.mButtons[i]);
                mButtons.Add(temp);

                // Select the first Button by default.
                if (mCurrentButton == null)
                {
                    mCurrentButton = temp;
                }
                else
                {
                    // If this isn't the first Button than link it to the previous one.
                    temp.LinkLeft(mButtons[i - 1]);
                }
            }

            mSetActiveAnimationMsg = new SpriteRender.SetActiveAnimationMessage();
            mOnPopupCloseMsg = new OnPopupClosedMessage();
        }

        /// <summary>
        /// Called at the end of the frame where mParentGOH was added to the GameObjectManager.
        /// </summary>
        public override void OnAdd()
        {
            // Buttons also need to do some special handling during Add and Remove phase.
            for (Int32 i = 0; i < mButtons.Count; i++)
            {
                mButtons[i].OnAdd();
            }

            // Move the cursor to the currently highlighted Button.
            mCursor.pPosition = mCurrentButton.pGameObject.pPosition;

            // Give the initially highlighted button a chance to update itself to look highlighted.
            mCurrentButton.OnRollOver();

            // Push these onto the GameObjectManager so that we don't need to worry about updating and
            // rendering.
            GameObjectManager.pInstance.Add(mWindow);
            GameObjectManager.pInstance.Add(mCursor);
        }

        /// <summary>
        /// Called at the end of the frame on which this Behaviour's mParentGOH was removed from
        /// the GameObjectManager.
        /// </summary>
        public override void OnRemove()
        {
            // Buttons also need to do some special handling during Add and Remove phase.
            for (Int32 i = 0; i < mButtons.Count; i++)
            {
                mButtons[i].OnRemove();
            }

            GameObjectManager.pInstance.Remove(mWindow);
            GameObjectManager.pInstance.Remove(mCursor);
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            if (InputManager.pInstance.CheckAction(InputManager.InputActions.LA_LEFT, true))
            {
                // If the current Button has another Button linked to the Left, that Button will now
                // become the current button.
                if (null != mCurrentButton.pLeft)
                {
                    mCurrentButton.OnRollOff();
                    mCurrentButton = mCurrentButton.pLeft;
                    mCurrentButton.OnRollOver();

                    // Update the cursor to the next position.
                    mCursor.pPosition = mCurrentButton.pGameObject.pPosition;
                }
            } 
            else if (InputManager.pInstance.CheckAction(InputManager.InputActions.LA_RIGHT, true))
            {
                if (null != mCurrentButton.pRight)
                {
                    mCurrentButton.OnRollOff();
                    mCurrentButton = mCurrentButton.pRight;
                    mCurrentButton.OnRollOver();

                    mCursor.pPosition = mCurrentButton.pGameObject.pPosition;
                }
            }
            else if (InputManager.pInstance.CheckAction(InputManager.InputActions.A, true))
            {
                // Start updating all the regular GameObject again.
                GameObjectManager.pInstance.pCurUpdatePass = BehaviourDefinition.Passes.DEFAULT;

                // Remove ourseleves from the game.
                GameObjectManager.pInstance.Remove(mParentGOH);

                // Let the Game know what Button was actually pressed.
                mOnPopupCloseMsg.mSelection_In = mCurrentButton.pOnCloseSelection;
                GameObjectManager.pInstance.BroadcastMessage(mOnPopupCloseMsg, mParentGOH);
            }
            else if (InputManager.pInstance.CheckAction(InputManager.InputActions.B, true))
            {
                // Start updating all the regular GameObject again.
                GameObjectManager.pInstance.pCurUpdatePass = BehaviourDefinition.Passes.DEFAULT;

                // Remove ourseleves from the game.
                GameObjectManager.pInstance.Remove(mParentGOH);

                // Let the Game know that no Button was pressed.
                mOnPopupCloseMsg.mSelection_In = StrandedPopupDefinition.ButtonTypes.None;
                GameObjectManager.pInstance.BroadcastMessage(mOnPopupCloseMsg);
            }
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        /// <param name="effect">The effect being used to render this object.</param>
        public override void Render(SpriteBatch batch, Effect effect)
        {
            /// <todo>
            /// Should the hint text be moved into its own GameObject so that we don't need to do
            /// the rendering ourselves?
            /// </todo>
             

            String msg = mCurrentButton.pHintText;

            // The size of a single character, used to calculate offset needed for centering.
            Single characterWidth = 8.0f;

            // This GameObject is positioned at the center of the screen, but needs to be offset based on the
            // number of characters in the current score.
            // First find the number of characters needed to offset, which is half of the string.
            // Then multiply that by the size of a single character.
            // Finally negate the number since we want to move left, not right.
            Single centerOffset = msg.Length * 0.5f * characterWidth * -1.0f;

            Single xPos = centerOffset + mWindow.pPosition.X;
            batch.DrawString(mFont, msg, new Vector2(xPos, mWindow.pPosition.Y + 1), Color.Black);
            batch.DrawString(mFont, msg, new Vector2(xPos, mWindow.pPosition.Y), Color.White);
        }

        /// <summary>
        /// The main interface for communicating between behaviours.  Using polymorphism, we
        /// define a bunch of different messages deriving from BehaviourMessage.  Each behaviour
        /// can then check for particular upcasted messahe types, and either grab some data 
        /// from it (set message) or store some data in it (get message).
        /// </summary>
        /// <param name="msg">The message being communicated to the behaviour.</param>
        public override void OnMessage(ref BehaviourMessage msg)
        {
        }

        /// <summary>
        /// Resets a behaviour to its initial state.
        /// </summary>
        public override void Reset()
        {
            mCurrentButton = mButtons[0];
        }
    }
}
