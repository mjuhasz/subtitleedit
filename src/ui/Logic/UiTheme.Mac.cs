using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nikse.SubtitleEdit.Logic.Config;
using System;
using System.Linq;

namespace Nikse.SubtitleEdit.Logic;

public static partial class UiTheme
{
    private static readonly Geometry MacPopupChevronGeometry = Geometry.Parse("M 0.5 0.5 L 4.5 4 L 0.5 7.5");

    private readonly record struct MacButtonPalette(
        Color Background,
        Color Border,
        Color Foreground,
        Color HoverBackground,
        Color PressedBackground,
        Color DisabledBackground,
        Color DisabledForeground);

    private readonly record struct PopupMenuPalette(
        Color Background,
        Color Border,
        Color Foreground,
        Color HoverBackground,
        Color HoverForeground,
        Color PressedBackground,
        Color PressedForeground,
        Color Separator,
        Color DisabledForeground);

    private static Styles? _macButtonStyle;
    private static IStyle? _macMenuChromeStyle;
    private static ResourceDictionary? _macMenuChromeResources;
    private static bool _macSpinnerTemplateHandlerRegistered;
    private static bool _macComboBoxTemplateHandlerRegistered;

    static partial void ApplyMacMenuChromeStyle(double factor)
    {
        if (Application.Current == null)
        {
            return;
        }

        if (_macMenuChromeStyle != null)
        {
            Application.Current.Styles.Remove(_macMenuChromeStyle);
            _macMenuChromeStyle = null;
        }

        if (_macMenuChromeResources != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(_macMenuChromeResources);
            _macMenuChromeResources = null;
        }

        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var metrics = GetPopupMenuMetrics(factor);
        var palette = GetPopupMenuPalette();
        var styles = new Styles();
        var transparentBrush = Brushes.Transparent;
        var backgroundBrush = new SolidColorBrush(palette.Background);
        var foregroundBrush = new SolidColorBrush(palette.Foreground);
        var borderBrush = new SolidColorBrush(palette.Border);
        var hoverBrush = new SolidColorBrush(palette.HoverBackground);
        var hoverForegroundBrush = new SolidColorBrush(palette.HoverForeground);
        var pressedBrush = new SolidColorBrush(palette.PressedBackground);
        var pressedForegroundBrush = new SolidColorBrush(palette.PressedForeground);
        var separatorBrush = new SolidColorBrush(palette.Separator);
        var disabledBrush = new SolidColorBrush(palette.DisabledForeground);
        styles.Add(new Style(x => x.OfType<MenuFlyoutPresenter>())
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, backgroundBrush),
                new Setter(TemplatedControl.ForegroundProperty, foregroundBrush),
                new Setter(TemplatedControl.BorderBrushProperty, borderBrush),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
                new Setter(TemplatedControl.CornerRadiusProperty, metrics.PresenterCornerRadius),
                new Setter(TemplatedControl.PaddingProperty, metrics.PresenterPadding),
                new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Left),
            }
        });

        styles.Add(new Style(x => x.OfType<ContextMenu>())
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, backgroundBrush),
                new Setter(TemplatedControl.ForegroundProperty, foregroundBrush),
                new Setter(TemplatedControl.BorderBrushProperty, borderBrush),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
                new Setter(TemplatedControl.CornerRadiusProperty, metrics.PresenterCornerRadius),
                new Setter(TemplatedControl.PaddingProperty, metrics.PresenterPadding),
            }
        });

        AddMacMenuDescendantStyles(styles, x => x.OfType<MenuFlyoutPresenter>(),
            metrics, transparentBrush, foregroundBrush, hoverForegroundBrush, pressedForegroundBrush, disabledBrush, separatorBrush);
        AddMacMenuDescendantStyles(styles, x => x.OfType<ContextMenu>(),
            metrics, transparentBrush, foregroundBrush, hoverForegroundBrush, pressedForegroundBrush, disabledBrush, separatorBrush);

        // The Fluent ControlTheme for MenuItem uses DynamicResource keys inside
        // the template's :pointerover/:pressed pseudo-class styles — they target
        // template-internal parts directly and override TemplateBinding Background.
        // A Style setting MenuItem.Background has no effect; we must override the
        // resource keys the template reads.
        _macMenuChromeResources = new ResourceDictionary
        {
            ["MenuFlyoutItemBackgroundPointerOver"] = hoverBrush,
            ["MenuFlyoutItemForegroundPointerOver"] = hoverForegroundBrush,
            ["MenuFlyoutItemBackgroundPressed"] = pressedBrush,
            ["MenuFlyoutItemForegroundPressed"] = pressedForegroundBrush,
        };
        Application.Current.Resources.MergedDictionaries.Add(_macMenuChromeResources);

        _macMenuChromeStyle = styles;
        Application.Current.Styles.Add(styles);
    }

    private static void AddMacMenuDescendantStyles(
        Styles styles,
        Func<Selector?, Selector> root,
        PopupMenuMetrics metrics,
        IBrush transparentBrush,
        IBrush foregroundBrush,
        IBrush hoverForegroundBrush,
        IBrush pressedForegroundBrush,
        IBrush disabledBrush,
        IBrush separatorBrush)
    {
        styles.Add(new Style(x => root(x).Descendant().OfType<MenuItem>())
        {
            Setters =
            {
                new Setter(MenuItem.FontSizeProperty, metrics.FontSize),
                new Setter(MenuItem.MinHeightProperty, metrics.MinHeight),
                new Setter(MenuItem.PaddingProperty, metrics.ItemPadding),
                new Setter(MenuItem.MarginProperty, metrics.ItemMargin),
                new Setter(TemplatedControl.CornerRadiusProperty, metrics.ItemCornerRadius),
                new Setter(TemplatedControl.BackgroundProperty, transparentBrush),
                new Setter(TemplatedControl.ForegroundProperty, foregroundBrush),
            }
        });
        // Background is NOT set here — Fluent's ControlTheme targets the internal Border
        // directly via DynamicResource(MenuFlyoutItemBackgroundPointerOver/Pressed), so
        // a style setter on MenuItem.Background has no effect. The resource dictionary
        // override in ApplyMacMenuChromeStyle is what actually changes the hover/pressed
        // background. Foreground works because Fluent sets it on the MenuItem itself.
        styles.Add(new Style(x => root(x).Descendant().OfType<MenuItem>().Class(":pointerover"))
        {
            Setters = { new Setter(TemplatedControl.ForegroundProperty, hoverForegroundBrush) }
        });
        styles.Add(new Style(x => root(x).Descendant().OfType<MenuItem>().Class(":pressed"))
        {
            Setters = { new Setter(TemplatedControl.ForegroundProperty, pressedForegroundBrush) }
        });
        styles.Add(new Style(x => root(x).Descendant().OfType<MenuItem>().Class(":disabled"))
        {
            Setters = { new Setter(TemplatedControl.ForegroundProperty, disabledBrush) }
        });
        styles.Add(new Style(x => root(x).Descendant().OfType<Separator>())
        {
            Setters =
            {
                new Setter(Layoutable.MarginProperty, metrics.SeparatorMargin),
                new Setter(TemplatedControl.BackgroundProperty, separatorBrush),
                new Setter(Layoutable.MinHeightProperty, 1.0),
            }
        });
    }

    // Avalonia's default submenu indicator is noticeably larger than the native
    // macOS chevron here, and the usual style/resource hooks did not affect this
    // runtime path. Mutate the realized chevron Path on the specific popup items
    // that opt in, while still keeping the upstream MenuItem template intact.
    public static void AttachMacPopupChevronOverride(MenuItem item)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        item.TemplateApplied -= OnMacPopupMenuTemplateApplied;
        item.TemplateApplied += OnMacPopupMenuTemplateApplied;
        item.SubmenuOpened -= OnMacPopupSubmenuOpened;
        item.SubmenuOpened += OnMacPopupSubmenuOpened;
        // Watch IsSubMenuOpen to revert the parent to gray when its popup is open.
        // Avalonia keeps :pointerover on the parent while the child popup is visible,
        // so the MenuFlyoutItemBackgroundPointerOver resource stays active.
        // Only a local SetValue on the template-internal background Border wins over it.
        item.PropertyChanged -= OnMacPopupItemPropertyChanged;
        item.PropertyChanged += OnMacPopupItemPropertyChanged;
        ScheduleMacPopupChevronOverride(item);
    }

    private static void OnMacPopupMenuTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (sender is MenuItem item)
        {
            ScheduleMacPopupChevronOverride(item);
        }
    }

    private static void OnMacPopupSubmenuOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item)
        {
            ScheduleMacPopupChevronOverride(item);
        }
    }

    private static void OnMacPopupItemPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != MenuItem.IsSubMenuOpenProperty || sender is not MenuItem item)
            return;
        var isOpen = e.GetNewValue<bool>();
        Dispatcher.UIThread.Post(() => ApplyMacMenuItemOpenState(item, isOpen), DispatcherPriority.Loaded);
    }

    private static void ApplyMacMenuItemOpenState(MenuItem item, bool isOpen)
    {
        // Fluent11's MenuItem template has no named background element (PART_LayoutRoot
        // or PART_RootPanel don't exist here). We find it by position: the first Border
        // inside the template root, or the template root itself if it is a Border.
        // Setting a local value on that element overrides the DynamicResource that the
        // ControlTheme's :pointerover style sets on it.
        var bgBorder = FindMenuItemBackgroundBorder(item);

        if (isOpen)
        {
            var isDark = IsDarkThemeEnabled();
            var grayColor = isDark
                ? UiUtil.LightenColor(GetDarkThemeBackgroundColor(), 22)
                : Color.FromRgb(218, 218, 222);

            bgBorder?.SetValue(Border.BackgroundProperty, new SolidColorBrush(grayColor));

            // In light mode only: revert text and chevron to dark on the gray background,
            // matching native macOS. The chevron follows item.ForegroundProperty via its
            // observable binding. The ContentPresenter's Foreground is bound directly to
            // DynamicResource(MenuFlyoutItemForegroundPointerOver) inside the template, so
            // it needs its own local value — setting it on the item alone is not enough.
            if (!isDark)
            {
                var darkBrush = new SolidColorBrush(Color.FromRgb(32, 32, 36));
                item.SetValue(TemplatedControl.ForegroundProperty, darkBrush);
                item.GetVisualDescendants()
                    .OfType<ContentPresenter>()
                    .FirstOrDefault()
                    ?.SetValue(TextElement.ForegroundProperty, darkBrush);
            }
        }
        else
        {
            bgBorder?.ClearValue(Border.BackgroundProperty);
            if (!IsDarkThemeEnabled())
            {
                item.ClearValue(TemplatedControl.ForegroundProperty);
                item.GetVisualDescendants()
                    .OfType<ContentPresenter>()
                    .FirstOrDefault()
                    ?.ClearValue(TextElement.ForegroundProperty);
            }
        }
    }

    private static Border? FindMenuItemBackgroundBorder(MenuItem item)
    {
        // Strategy 1: template root is itself a Border (root = background element).
        var templateRoot = item.GetVisualChildren().FirstOrDefault();
        if (templateRoot is Border rootBorder)
            return rootBorder;

        // Strategy 2: template root is a Panel/Grid; background Border is its first child.
        if (templateRoot != null)
        {
            var firstBorder = templateRoot.GetVisualChildren().OfType<Border>().FirstOrDefault();
            if (firstBorder != null)
                return firstBorder;
        }

        // Strategy 3: deeper search — first Border anywhere in the visual subtree.
        return item.GetVisualDescendants().OfType<Border>().FirstOrDefault();
    }

    private static void ScheduleMacPopupChevronOverride(MenuItem item)
    {
        Dispatcher.UIThread.Post(() => ApplyMacPopupChevronOverride(item), DispatcherPriority.Loaded);
    }

    private static void ApplyMacPopupChevronOverride(MenuItem item)
    {
        if (!item.HasSubMenu)
        {
            return;
        }

        var chevron = item.GetVisualDescendants()
            .OfType<Path>()
            .FirstOrDefault(p => p.Name == "PART_ChevronPath");

        if (chevron == null)
        {
            return;
        }

        var factor = Se.Settings.Appearance.LayoutScale;
        chevron.Data = MacPopupChevronGeometry;
        chevron.Stretch = Stretch.None;
        chevron.Width = 5 * factor;
        chevron.Height = 8 * factor;
        chevron.Margin = new Thickness(9 * factor, 0, 4 * factor, 0);
        chevron.Bind(Shape.StrokeProperty, item.GetObservable(TemplatedControl.ForegroundProperty));
        chevron.StrokeThickness = 1.25;
        chevron.StrokeJoin = PenLineJoin.Round;
        chevron.StrokeLineCap = PenLineCap.Round;
        chevron.Fill = null;
    }

    private static PopupMenuPalette GetPopupMenuPalette()
    {
        var highlight = GetMacMenuHighlightColor();
        var highlightPressed = Color.FromArgb(highlight.A,
            (byte)(highlight.R * 85 / 100),
            (byte)(highlight.G * 85 / 100),
            (byte)(highlight.B * 85 / 100));

        if (!IsDarkThemeEnabled())
        {
            return new PopupMenuPalette(
                Background: Color.FromRgb(246, 246, 248),
                Border: Color.FromRgb(220, 220, 224),
                Foreground: Color.FromRgb(32, 32, 36),
                HoverBackground: highlight,
                HoverForeground: Colors.White,
                PressedBackground: highlightPressed,
                PressedForeground: Colors.White,
                Separator: Color.FromRgb(216, 216, 221),
                DisabledForeground: Color.FromRgb(156, 156, 160));
        }

        var baseColor = GetDarkThemeBackgroundColor();
        return new PopupMenuPalette(
            Background: UiUtil.LightenColor(baseColor, 10),
            Border: UiUtil.LightenColor(baseColor, 24),
            Foreground: GetDarkThemeForegroundColor(),
            HoverBackground: highlight,
            HoverForeground: Colors.White,
            PressedBackground: highlightPressed,
            PressedForeground: Colors.White,
            Separator: UiUtil.LightenColor(baseColor, 20),
            DisabledForeground: Color.FromRgb(142, 142, 147));
    }

    static partial void ApplyMacButtonStyle()
    {
        if (Application.Current == null)
        {
            return;
        }

        if (_macButtonStyle != null)
        {
            Application.Current.Styles.Remove(_macButtonStyle);
            _macButtonStyle = null;
        }

        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        EnsureMacSpinnerTemplateHandler();
        EnsureMacComboBoxTemplateHandler();

        var palette = GetMacButtonPalette();
        var accentColor = GetMacAccentColor();

        var bg         = new SolidColorBrush(palette.Background);
        var border     = new SolidColorBrush(palette.Border);
        var fg         = new SolidColorBrush(palette.Foreground);
        var hover      = new SolidColorBrush(palette.HoverBackground);
        var pressed    = new SolidColorBrush(palette.PressedBackground);
        var disabledBg = new SolidColorBrush(palette.DisabledBackground);
        var disabledFg = new SolidColorBrush(palette.DisabledForeground);
        var accent     = new SolidColorBrush(accentColor);
        var accentHover = new SolidColorBrush(UiUtil.LightenColor(accentColor, 20));
        var accentPressed = new SolidColorBrush(Color.FromArgb(
            accentColor.A,
            (byte)(accentColor.R * 85 / 100),
            (byte)(accentColor.G * 85 / 100),
            (byte)(accentColor.B * 85 / 100)));

        var cornerRadius    = new CornerRadius(6);
        var borderThickness = new Thickness(1);
        var styles          = new Styles();

        // Base: all buttons get macOS-style look; suppress Fluent's rectangular focus adorner
        styles.Add(new Style(x => x.OfType<Button>())
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty,      bg),
                new Setter(Button.BorderBrushProperty,     border),
                new Setter(Button.BorderThicknessProperty, borderThickness),
                new Setter(Button.ForegroundProperty,      fg),
                new Setter(Button.CornerRadiusProperty,    cornerRadius),
                new Setter(Control.FocusAdornerProperty, null),
            }
        });
        styles.Add(new Style(x => x.OfType<Button>().Class(":pointerover"))
        {
            Setters = { new Setter(Button.BackgroundProperty, hover) }
        });
        styles.Add(new Style(x => x.OfType<Button>().Class(":pressed"))
        {
            Setters = { new Setter(Button.BackgroundProperty, pressed) }
        });

        // Keyboard focus: fill with accent color instead of the rectangle ring
        styles.Add(new Style(x => x.OfType<Button>().Class(":focus-visible"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, accent),
                new Setter(Button.ForegroundProperty, Brushes.White),
            }
        });
        styles.Add(new Style(x => x.OfType<Button>().Class(":disabled"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, disabledBg),
                new Setter(Button.ForegroundProperty, disabledFg),
            }
        });

        styles.Add(new Style(x => x.OfType<SplitButton>())
        {
            Setters =
            {
                new Setter(SplitButton.BackgroundProperty,      bg),
                new Setter(SplitButton.BorderBrushProperty,     border),
                new Setter(SplitButton.BorderThicknessProperty, borderThickness),
                new Setter(SplitButton.ForegroundProperty,      fg),
                new Setter(SplitButton.CornerRadiusProperty,    cornerRadius),
                new Setter(Control.FocusAdornerProperty, null),
            }
        });
        styles.Add(new Style(x => x.OfType<SplitButton>().Class(":pointerover"))
        {
            Setters = { new Setter(SplitButton.BackgroundProperty, hover) }
        });
        styles.Add(new Style(x => x.OfType<SplitButton>().Class(":pressed"))
        {
            Setters = { new Setter(SplitButton.BackgroundProperty, pressed) }
        });
        styles.Add(new Style(x => x.OfType<SplitButton>().Class(":focus-visible"))
        {
            Setters =
            {
                new Setter(SplitButton.BackgroundProperty, accent),
                new Setter(SplitButton.ForegroundProperty, Brushes.White),
            }
        });
        styles.Add(new Style(x => x.OfType<SplitButton>().Class(":disabled"))
        {
            Setters =
            {
                new Setter(SplitButton.BackgroundProperty, disabledBg),
                new Setter(SplitButton.ForegroundProperty, disabledFg),
            }
        });

        // Toolbar buttons: frameless at rest, subtle translucent hover/press overlay
        styles.Add(new Style(x => x.OfType<Button>().Class("toolbar"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty,      Brushes.Transparent),
                new Setter(Button.BorderBrushProperty,     Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
            }
        });
        styles.Add(new Style(x => x.OfType<Button>().Class("toolbar").Class(":pointerover"))
        {
            Setters = { new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromArgb(18, 0, 0, 0))) }
        });
        styles.Add(new Style(x => x.OfType<Button>().Class("toolbar").Class(":pressed"))
        {
            Setters = { new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromArgb(36, 0, 0, 0))) }
        });

        styles.Add(new Style(x => x.OfType<SplitButton>().Class("toolbar"))
        {
            Setters =
            {
                new Setter(SplitButton.BackgroundProperty,      Brushes.Transparent),
                new Setter(SplitButton.BorderBrushProperty,     Brushes.Transparent),
                new Setter(SplitButton.BorderThicknessProperty, new Thickness(0)),
            }
        });
        styles.Add(new Style(x => x.OfType<SplitButton>().Class("toolbar").Class(":pointerover"))
        {
            Setters = { new Setter(SplitButton.BackgroundProperty, new SolidColorBrush(Color.FromArgb(18, 0, 0, 0))) }
        });
        styles.Add(new Style(x => x.OfType<SplitButton>().Class("toolbar").Class(":pressed"))
        {
            Setters = { new Setter(SplitButton.BackgroundProperty, new SolidColorBrush(Color.FromArgb(36, 0, 0, 0))) }
        });

        // Restore accent button appearance on top of the base gray above
        styles.Add(new Style(x => x.OfType<Button>().Class("accent"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, accent),
                new Setter(Button.ForegroundProperty, Brushes.White),
            }
        });
        styles.Add(new Style(x => x.OfType<Button>().Class("accent").Class(":pointerover"))
        {
            Setters = { new Setter(Button.BackgroundProperty, accentHover) }
        });
        styles.Add(new Style(x => x.OfType<Button>().Class("accent").Class(":pressed"))
        {
            Setters = { new Setter(Button.BackgroundProperty, accentPressed) }
        });

        styles.Add(new Style(x => x.OfType<SplitButton>().Class("accent"))
        {
            Setters =
            {
                new Setter(SplitButton.BackgroundProperty, accent),
                new Setter(SplitButton.ForegroundProperty, Brushes.White),
            }
        });
        styles.Add(new Style(x => x.OfType<SplitButton>().Class("accent").Class(":pointerover"))
        {
            Setters = { new Setter(SplitButton.BackgroundProperty, accentHover) }
        });
        styles.Add(new Style(x => x.OfType<SplitButton>().Class("accent").Class(":pressed"))
        {
            Setters = { new Setter(SplitButton.BackgroundProperty, accentPressed) }
        });

        _macButtonStyle = styles;
        Application.Current.Styles.Add(styles);
    }

    private static void EnsureMacSpinnerTemplateHandler()
    {
        if (_macSpinnerTemplateHandlerRegistered)
        {
            return;
        }

        _macSpinnerTemplateHandlerRegistered = true;
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<ButtonSpinner>((spinner, e) =>
        {
            if (e is TemplateAppliedEventArgs)
            {
                Dispatcher.UIThread.Post(() => ApplyMacSpinnerArrowOverride(spinner), DispatcherPriority.Loaded);
            }
        });
    }

    private static void EnsureMacComboBoxTemplateHandler()
    {
        if (_macComboBoxTemplateHandlerRegistered)
            return;
        _macComboBoxTemplateHandlerRegistered = true;
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<ComboBox>((cb, e) =>
        {
            if (e is TemplateAppliedEventArgs)
                Dispatcher.UIThread.Post(() => ApplyMacComboBoxArrowOverride(cb), DispatcherPriority.Loaded);
        });
    }

    private static Color GetMacAccentColor()
    {
        Application.Current!.TryGetResource(
            "SystemAccentColor",
            Application.Current.ActualThemeVariant,
            out var raw);
        return raw is Color c ? c : Color.FromRgb(0, 122, 255);
    }

    // Returns the colour to use for menu-item hover highlights.
    // In light mode, Fluent's SystemAccentColorLight1 is a close approximation of
    // native NSMenu's accent-over-vibrancy rendering. In dark mode Light1 is computed
    // too bright by the dark-theme palette; the raw accent (#007AFF) reads better there.
    private static Color GetMacMenuHighlightColor()
    {
        if (!IsDarkThemeEnabled())
        {
            Application.Current!.TryGetResource(
                "SystemAccentColorLight1",
                Application.Current.ActualThemeVariant,
                out var raw);
            if (raw is Color c)
                return c;
            return UiUtil.LightenColor(GetMacAccentColor(), 20);
        }
        return GetMacAccentColor();
    }

    private static MacButtonPalette GetMacButtonPalette()
    {
        if (!IsDarkThemeEnabled())
        {
            return new MacButtonPalette(
                Background:         Color.FromRgb(232, 232, 237),
                Border:             Color.FromRgb(209, 209, 214),
                Foreground:         Color.FromRgb(0,   0,   0),
                HoverBackground:    Color.FromRgb(218, 218, 224),
                PressedBackground:  Color.FromRgb(204, 204, 210),
                DisabledBackground: Color.FromRgb(236, 236, 240),
                DisabledForeground: Color.FromRgb(170, 170, 174));
        }

        var baseColor = GetDarkThemeBackgroundColor();
        return new MacButtonPalette(
            Background:         UiUtil.LightenColor(baseColor, 12),
            Border:             UiUtil.LightenColor(baseColor, 18),
            Foreground:         GetDarkThemeForegroundColor(),
            HoverBackground:    UiUtil.LightenColor(baseColor, 20),
            PressedBackground:  UiUtil.LightenColor(baseColor, 28),
            DisabledBackground: UiUtil.LightenColor(baseColor,  6),
            DisabledForeground: Color.FromRgb(100, 100, 104));
    }

    // Two stacked open-V chevrons matching NSPopUpButton's double-arrow indicator.
    // Same stroke-based idiom as MacPopupChevronGeometry — no fill.
    private static readonly Geometry MacComboBoxArrowGeometry =
        Geometry.Parse("M 2 5 L 5.5 1.5 L 9 5 M 2 9 L 5.5 12.5 L 9 9");

    // Avalonia Fluent ComboBox bakes a single down-chevron PathIcon into its toggle
    // button. The usual style/resource overrides don't reach it, so we mutate the
    // realized visual tree after template application, exactly like the submenu
    // chevron override above.
    public static void AttachMacComboBoxArrowOverride(ComboBox cb)
    {
        if (!OperatingSystem.IsMacOS())
            return;

        cb.TemplateApplied -= OnMacComboBoxTemplateApplied;
        cb.TemplateApplied += OnMacComboBoxTemplateApplied;
        Dispatcher.UIThread.Post(() => ApplyMacComboBoxArrowOverride(cb), DispatcherPriority.Loaded);
    }

    private static void OnMacComboBoxTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (sender is ComboBox cb)
            Dispatcher.UIThread.Post(() => ApplyMacComboBoxArrowOverride(cb), DispatcherPriority.Loaded);
    }

    private static void ApplyMacComboBoxArrowOverride(ComboBox cb)
    {
        // "ChevronIcon" is the Fluent11 name; "DropDownGlyph" and "Arrow" are
        // used in older Avalonia / alternate themes.
        var pathIcon = cb.GetVisualDescendants()
            .OfType<PathIcon>()
            .FirstOrDefault(p => p.Name is "ChevronIcon" or "DropDownGlyph" or "Arrow");

        if (pathIcon == null)
        {
            // Fallback: last PathIcon inside the ToggleButton is the dropdown indicator.
            var toggle = cb.GetVisualDescendants().OfType<ToggleButton>().FirstOrDefault();
            pathIcon = toggle?.GetVisualDescendants().OfType<PathIcon>().LastOrDefault();
        }

        if (pathIcon == null)
            return;

        Panel? visualParent = null;
        var current = pathIcon.GetVisualParent();
        while (current != null && current is not (ToggleButton or ComboBox))
        {
            if (current is Panel p) { visualParent = p; break; }
            current = current.GetVisualParent();
        }
        if (visualParent == null)
            return;

        // Remove any stale overlay from a previous template cycle.
        var existing = visualParent.Children.OfType<Path>().FirstOrDefault(p => p.Name == "MacComboArrow");
        if (existing != null)
            visualParent.Children.Remove(existing);

        pathIcon.IsVisible = false;

        var arrow = new Path
        {
            Name = "MacComboArrow",
            Data = MacComboBoxArrowGeometry,
            Stretch = Stretch.None,
            Width = 11,
            Height = 14,
            Fill = null,
            StrokeThickness = 1.2,
            StrokeJoin = PenLineJoin.Round,
            StrokeLineCap = PenLineCap.Round,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };
        arrow.Bind(Shape.StrokeProperty, cb.GetObservable(TemplatedControl.ForegroundProperty));

        if (visualParent is Grid)
        {
            Grid.SetColumn(arrow, Grid.GetColumn(pathIcon));
            Grid.SetRow(arrow, Grid.GetRow(pathIcon));
        }
        visualParent.Children.Add(arrow);
    }

    // Single open-V chevrons for the stepper up/down buttons — stroke-based, no fill,
    // same idiom as the ComboBox and popup menu overrides above.
    private const double MacSpinnerButtonWidth = 28;
    private static readonly Geometry MacSpinnerUpGeometry   = Geometry.Parse("M 1.5 6 L 5 2 L 8.5 6");
    private static readonly Geometry MacSpinnerDownGeometry = Geometry.Parse("M 1.5 2 L 5 6 L 8.5 2");

    private static void ApplyMacSpinnerArrowOverride(ButtonSpinner spinner)
    {
        var increase = spinner.GetVisualDescendants()
            .OfType<RepeatButton>()
            .FirstOrDefault(b => b.Name == "PART_IncreaseButton");
        var decrease = spinner.GetVisualDescendants()
            .OfType<RepeatButton>()
            .FirstOrDefault(b => b.Name == "PART_DecreaseButton");

        ApplySpinnerButtonWidth(increase);
        ApplySpinnerButtonWidth(decrease);
        ApplySpinnerChevron(increase, MacSpinnerUpGeometry,   "MacSpinnerUp");
        ApplySpinnerChevron(decrease, MacSpinnerDownGeometry, "MacSpinnerDown");
    }

    private static void ApplySpinnerButtonWidth(RepeatButton? btn)
    {
        if (btn == null)
            return;

        btn.Width = MacSpinnerButtonWidth;
        btn.MinWidth = MacSpinnerButtonWidth;
        btn.MaxWidth = MacSpinnerButtonWidth;
    }

    private static void ApplySpinnerChevron(RepeatButton? btn, Geometry chevron, string overlayName)
    {
        if (btn == null)
            return;

        // Skip if our arrow is already installed (avoids redundant reattaches).
        if (btn.Content is Path existing && existing.Name == overlayName)
            return;

        // The Fluent RepeatButton template is Border → ContentPresenter → (content).
        // There is no Panel in that chain, so the old approach of finding a Panel and
        // adding a sibling overlay never worked. Replace Content directly instead —
        // local values beat template-set values in Avalonia's property system, and
        // the TemplateApplied subscription in AttachMacSpinnerArrowStyle re-runs this
        // whenever the ButtonSpinner template is reapplied.
        var arrow = new Path
        {
            Name = overlayName,
            Data = chevron,
            Stretch = Stretch.None,
            Width = 10,
            Height = 8,
            Fill = null,
            StrokeThickness = 1.25,
            StrokeJoin = PenLineJoin.Round,
            StrokeLineCap = PenLineCap.Round,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };
        arrow.Bind(Shape.StrokeProperty, btn.GetObservable(TemplatedControl.ForegroundProperty));
        btn.Content = arrow;
    }

}
