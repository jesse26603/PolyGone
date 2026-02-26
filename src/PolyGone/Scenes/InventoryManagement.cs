using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone
{
    public enum ItemType
    {
        DoubleJump,
        SpeedBoost,
        HealingGlow,
        MultiShot,
        RapidFire,
        LowGravity,
        IronWill
    }

    public enum WeaponType
    {
        Blaster,
        Shotgun,
        Rifle,
        Automatic,
        VoidLance
    }

    internal class InventoryManagement : IScene
    {
        private Texture2D? _pixel;
        private SpriteFont? _font;
        private KeyboardState keyboardState;
        private KeyboardState previousKeyboardState;
        private readonly ContentManager _content;
        private readonly SceneManager _sceneManager;
        private readonly GraphicsDeviceManager _graphics;
        private readonly string _levelFile;

        // Available items and weapons
        private readonly string[] _itemNames = 
        {
            "Double Jump",
            "Speed Boost",
            "Healing Glow",
            "Multi-Shot",
            "Rapid Fire",
            "Low Gravity",
            "Iron Will"
        };
        private readonly ItemType[] _itemTypes = 
        {
            ItemType.DoubleJump,
            ItemType.SpeedBoost,
            ItemType.HealingGlow,
            ItemType.MultiShot,
            ItemType.RapidFire,
            ItemType.LowGravity,
            ItemType.IronWill
        };
        private readonly string[] _itemDescriptions =
        {
            "Second jump in mid-air",
            "Move 50% faster",
            "Regen 10 HP every 2s",
            "Adds 3 extra pellets to the Shotgun",
            "Cooldown reduced to 1/3",
            "Gravity reduced to 40%",
            "Survive 1 kill every 20s"
        };
        private readonly string[] _weaponNames = { "Blaster", "Shotgun", "Rifle", "Automatic", "Void Lance" };
        private readonly WeaponType[] _weaponTypes = { WeaponType.Blaster, WeaponType.Shotgun, WeaponType.Rifle, WeaponType.Automatic, WeaponType.VoidLance };

        // Static fields to remember last selection across instances
        private static List<ItemType> _lastSelectedItems = new List<ItemType> { ItemType.DoubleJump, ItemType.SpeedBoost };
        private static WeaponType _lastSelectedWeapon = WeaponType.Blaster;

        // Selection state
        private enum SelectionMode { Items, Weapon, Confirm }
        private SelectionMode _currentMode = SelectionMode.Items;
        private int _itemCursor = 0;
        private int _weaponCursor = 0;
        private int _confirmCursor = 0; // 0 = Start Game, 1 = Back
        
        private readonly List<ItemType> _selectedItems;
        private WeaponType _selectedWeapon;

        public InventoryManagement(ContentManager content, SceneManager sceneManager, GraphicsDeviceManager graphics, string levelFile)
        {
            _pixel = null;
            _content = content;
            _sceneManager = sceneManager;
            _graphics = graphics;
            _levelFile = levelFile;
            previousKeyboardState = Keyboard.GetState();
            
            // Initialize with last selected values
            _selectedItems = new List<ItemType>(_lastSelectedItems);
            _selectedWeapon = _lastSelectedWeapon;
        }

        public void Load()
        {
            if (_font == null)
            {
                try
                {
                    _font = _content.Load<SpriteFont>("Fonts/PauseMenu");
                }
                catch
                {
                    // Font not available
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            keyboardState = Keyboard.GetState();

            // Check for Control key to skip inventory and start with current selections
            if (keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl))
            {
                // Start game with current selections (which were loaded from last time)
                StartGame();
                previousKeyboardState = keyboardState;
                return;
            }

            // Check for Escape key to go back to previous screen
            if (InputManager.IsEscapeKeyPressed())
            {
                _sceneManager.PopScene(this);
                previousKeyboardState = keyboardState;
                return;
            }

            // Mouse navigation for items
            HandleMouseNavigation();

            switch (_currentMode)
            {
                case SelectionMode.Items:
                    UpdateItemSelection();
                    break;
                case SelectionMode.Weapon:
                    UpdateWeaponSelection();
                    break;
                case SelectionMode.Confirm:
                    UpdateConfirmSelection();
                    break;
            }

            previousKeyboardState = keyboardState;
        }

        private void HandleMouseNavigation()
        {
            if (_font == null) return;

            var viewport = _graphics.GraphicsDevice.Viewport;

            // Check items section
            int itemsStartX = 100;
            int itemsStartY = 200;
            for (int i = 0; i < _itemNames.Length; i++)
            {
                var itemText = (_selectedItems.Contains(_itemTypes[i]) ? "[X] " : "[ ] ") + _itemNames[i];
                var textSize = _font.MeasureString(itemText);
                var bounds = new Rectangle(itemsStartX, itemsStartY + i * 40, (int)textSize.X, (int)textSize.Y);

                if (bounds.Contains(InputManager.GetMousePosition()))
                {
                    _currentMode = SelectionMode.Items;
                    _itemCursor = i;

                    if (InputManager.IsLeftMouseButtonClicked())
                    {
                        ItemType selectedItem = _itemTypes[_itemCursor];
                        if (_selectedItems.Contains(selectedItem))
                        {
                            _selectedItems.Remove(selectedItem);
                        }
                        else if (_selectedItems.Count < 2)
                        {
                            _selectedItems.Add(selectedItem);
                        }
                        InputManager.ConsumeClick();
                    }
                }
            }

            // Check weapons section
            int weaponsStartX = viewport.Width / 2;
            int weaponsStartY = 200;
            for (int i = 0; i < _weaponNames.Length; i++)
            {
                var weaponText = (_weaponTypes[i] == _selectedWeapon ? "(O) " : "( ) ") + _weaponNames[i];
                var textSize = _font.MeasureString(weaponText);
                var bounds = new Rectangle(weaponsStartX, weaponsStartY + i * 40, (int)textSize.X, (int)textSize.Y);

                if (bounds.Contains(InputManager.GetMousePosition()))
                {
                    _currentMode = SelectionMode.Weapon;
                    _weaponCursor = i;

                    if (InputManager.IsLeftMouseButtonClicked())
                    {
                        _selectedWeapon = _weaponTypes[_weaponCursor];
                        InputManager.ConsumeClick();
                    }
                }
            }

            // Check confirm section
            int confirmStartY = viewport.Height - 150;
            string[] confirmOptions = { "Start Game", "Back" };
            for (int i = 0; i < confirmOptions.Length; i++)
            {
                var optionText = (i == _confirmCursor && _currentMode == SelectionMode.Confirm ? "> " : "  ") + confirmOptions[i];
                var textSize = _font.MeasureString(optionText);
                var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, confirmStartY + i * 40);
                var bounds = new Rectangle((int)position.X, (int)position.Y, (int)textSize.X, (int)textSize.Y);

                if (bounds.Contains(InputManager.GetMousePosition()))
                {
                    _currentMode = SelectionMode.Confirm;
                    _confirmCursor = i;

                    if (InputManager.IsLeftMouseButtonClicked())
                    {
                        if (_confirmCursor == 0)
                        {
                            StartGame();
                        }
                        else
                        {
                            _sceneManager.PopScene(this);
                        }
                        InputManager.ConsumeClick();
                    }
                }
            }
        }

        private void UpdateItemSelection()
        {
            if (IsKeyPressed(Keys.Up))
            {
                _itemCursor = (_itemCursor - 1 + _itemNames.Length) % _itemNames.Length;
            }

            if (IsKeyPressed(Keys.Down))
            {
                _itemCursor = (_itemCursor + 1) % _itemNames.Length;
            }

            if (IsKeyPressed(Keys.Enter) || IsKeyPressed(Keys.Space))
            {
                ItemType selectedItem = _itemTypes[_itemCursor];
                
                if (_selectedItems.Contains(selectedItem))
                {
                    // Deselect item
                    _selectedItems.Remove(selectedItem);
                }
                else if (_selectedItems.Count < 2)
                {
                    // Select item (max 2)
                    _selectedItems.Add(selectedItem);
                }
            }

            if (IsKeyPressed(Keys.Right) || (IsKeyPressed(Keys.Tab) && !keyboardState.IsKeyDown(Keys.LeftShift)))
            {
                // Move to weapon selection
                _currentMode = SelectionMode.Weapon;
            }
        }

        private void UpdateWeaponSelection()
        {
            if (IsKeyPressed(Keys.Up))
            {
                _weaponCursor = (_weaponCursor - 1 + _weaponNames.Length) % _weaponNames.Length;
            }

            if (IsKeyPressed(Keys.Down))
            {
                _weaponCursor = (_weaponCursor + 1) % _weaponNames.Length;
            }

            if (IsKeyPressed(Keys.Enter) || IsKeyPressed(Keys.Space))
            {
                // Select weapon
                _selectedWeapon = _weaponTypes[_weaponCursor];
            }

            if (IsKeyPressed(Keys.Left) || (IsKeyPressed(Keys.Tab) && keyboardState.IsKeyDown(Keys.LeftShift)))
            {
                // Move back to item selection
                _currentMode = SelectionMode.Items;
            }

            if (IsKeyPressed(Keys.Right) || (IsKeyPressed(Keys.Tab) && !keyboardState.IsKeyDown(Keys.LeftShift)))
            {
                // Move to confirm
                _currentMode = SelectionMode.Confirm;
            }
        }

        private void UpdateConfirmSelection()
        {
            if (IsKeyPressed(Keys.Up) || IsKeyPressed(Keys.Down))
            {
                _confirmCursor = (_confirmCursor + 1) % 2;
            }

            if (IsKeyPressed(Keys.Enter))
            {
                if (_confirmCursor == 0)
                {
                    // Start Game
                    StartGame();
                }
                else
                {
                    // Back
                    _sceneManager.PopScene(this);
                }
            }

            if (IsKeyPressed(Keys.Left) || (IsKeyPressed(Keys.Tab) && keyboardState.IsKeyDown(Keys.LeftShift)))
            {
                // Move back to weapon selection
                _currentMode = SelectionMode.Weapon;
            }
        }

        private void StartGame()
        {
            // Save current selections for next time
            _lastSelectedItems = new List<ItemType>(_selectedItems);
            _lastSelectedWeapon = _selectedWeapon;
            
            // Pop this inventory management scene
            _sceneManager.PopScene(this);
            // Note: LevelSelect (or previous scene) remains on stack for proper back navigation
            // Add the new game scene
            _sceneManager.AddScene(new GameScene(_content, _sceneManager, _graphics, _levelFile, _selectedItems, _selectedWeapon));
            // Reset click cooldown when starting game
            InputManager.ResetClickCooldown();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }

            // Draw background
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height), Color.DarkBlue);

            if (_font != null)
            {
                var viewport = spriteBatch.GraphicsDevice.Viewport;
                
                // Draw title
                string title = "Select Your Loadout";
                var titleSize = _font.MeasureString(title);
                var titlePos = new Vector2(viewport.Width / 2f - titleSize.X / 2f, 50);
                spriteBatch.DrawString(_font, title, titlePos, Color.White);

                // Draw instructions
                string instructions = "Select up to 2 Items | Select 1 Weapon | Press Ctrl to skip";
                var instructionsSize = _font.MeasureString(instructions);
                var instructionsPos = new Vector2(viewport.Width / 2f - instructionsSize.X / 2f, 90);
                spriteBatch.DrawString(_font, instructions, instructionsPos, Color.Gray);

                // Draw items section
                DrawItemsSection(spriteBatch, viewport);

                // Draw weapons section
                DrawWeaponsSection(spriteBatch, viewport);

                // Draw confirm section
                DrawConfirmSection(spriteBatch, viewport);
            }
        }

        private void DrawItemsSection(SpriteBatch spriteBatch, Viewport viewport)
        {
            int startX = 100;
            int startY = 200;
            
            // Section title
            Color sectionColor = _currentMode == SelectionMode.Items ? Color.Yellow : Color.White;
            spriteBatch.DrawString(_font, "Items (choose 2):", new Vector2(startX, startY - 40), sectionColor);

            for (int i = 0; i < _itemNames.Length; i++)
            {
                string itemName = _itemNames[i];
                bool isSelected = _selectedItems.Contains(_itemTypes[i]);
                bool isCursor = i == _itemCursor && _currentMode == SelectionMode.Items;

                Color color = isCursor ? Color.Yellow : (isSelected ? Color.Green : Color.White);
                string prefix = isSelected ? "[X] " : "[ ] ";
                string description = isCursor ? $"  <- {_itemDescriptions[i]}" : "";
                
                spriteBatch.DrawString(_font, prefix + itemName + description, new Vector2(startX, startY + i * 40), color);
            }
        }

        private void DrawWeaponsSection(SpriteBatch spriteBatch, Viewport viewport)
        {
            int startX = viewport.Width / 2;
            int startY = 200;
            
            // Section title
            Color sectionColor = _currentMode == SelectionMode.Weapon ? Color.Yellow : Color.White;
            spriteBatch.DrawString(_font, "Weapon:", new Vector2(startX, startY - 40), sectionColor);

            for (int i = 0; i < _weaponNames.Length; i++)
            {
                string weaponName = _weaponNames[i];
                bool isSelected = _weaponTypes[i] == _selectedWeapon;
                bool isCursor = i == _weaponCursor && _currentMode == SelectionMode.Weapon;

                Color color = isCursor ? Color.Yellow : (isSelected ? Color.Green : Color.White);
                string prefix = isSelected ? "(O) " : "( ) ";
                
                spriteBatch.DrawString(_font, prefix + weaponName, new Vector2(startX, startY + i * 40), color);
            }
        }

        private void DrawConfirmSection(SpriteBatch spriteBatch, Viewport viewport)
        {
            if (_font == null) return;
            
            int startY = viewport.Height - 150;
            
            // Section title
            Color sectionColor = _currentMode == SelectionMode.Confirm ? Color.Yellow : Color.White;
            string sectionTitle = _currentMode == SelectionMode.Confirm ? "> Ready?" : "  Ready?";
            var sectionSize = _font.MeasureString(sectionTitle);
            spriteBatch.DrawString(_font, sectionTitle, new Vector2(viewport.Width / 2f - sectionSize.X / 2f, startY - 40), sectionColor);

            string[] confirmOptions = { "Start Game", "Back" };
            for (int i = 0; i < confirmOptions.Length; i++)
            {
                bool isCursor = i == _confirmCursor && _currentMode == SelectionMode.Confirm;
                Color color = isCursor ? Color.Yellow : Color.White;
                string prefix = isCursor ? "> " : "  ";
                
                var optionText = prefix + confirmOptions[i];
                var optionSize = _font.MeasureString(optionText);
                spriteBatch.DrawString(_font, optionText, new Vector2(viewport.Width / 2f - optionSize.X / 2f, startY + i * 40), color);
            }
        }

        private bool IsKeyPressed(Keys key)
        {
            return keyboardState.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
        }
    }
}
