using SDL = SDL3.SDL;

namespace lychee_game.resources;

using SDL3;
using SDL_WindowFlags = SDL.SDL_WindowFlags;

/// <summary>
/// Supported GPU rendering backends.
/// </summary>
public enum RenderingBackend
{
    /// <summary>Vulkan rendering backend.</summary>
    Vulkan,

    /// <summary>Direct3D 12 rendering backend.</summary>
    D3D12,

    /// <summary>Metal rendering backend (Apple platforms).</summary>
    Metal,
}

/// <summary>
/// Configuration for creating a Window.
/// </summary>
public sealed class WindowDescriptor
{
#region Public Properties

    /// <summary>Window title.</summary>
    public string Title { get; init; } = "lychee_game";

    /// <summary>Window width in pixels.</summary>
    public int Width { get; init; } = 1280;

    /// <summary>Window height in pixels.</summary>
    public int Height { get; init; } = 720;

    /// <summary>Whether the window starts in fullscreen mode.</summary>
    public bool Fullscreen { get; init; } = false;

    /// <summary>Whether the window is resizable.</summary>
    public bool Resizable { get; init; } = true;

    /// <summary>Whether the window has no borders/decorations.</summary>
    public bool Borderless { get; init; } = false;

    /// <summary>Whether the window starts hidden.</summary>
    public bool Hidden { get; init; } = false;

    /// <summary>Whether the mouse cursor is visible in the window.</summary>
    public bool MouseVisible { get; init; } = true;

    /// <summary>The GPU rendering backend to use.</summary>
    public RenderingBackend Backend { get; init; } = RenderingBackend.Vulkan;

    /// <summary>Whether the window uses high-DPI mode.</summary>
    public bool HighDpi { get; init; } = false;

    /// <summary>Whether the window stays on top of other windows.</summary>
    public bool AlwaysOnTop { get; init; } = false;

#endregion
}

/// <summary>
/// SDL3 window wrapper providing property access and lifecycle management.
/// </summary>
public sealed class Window : IDisposable
{
#region Public Properties

    /// <summary>Native SDL3 window handle.</summary>
    public IntPtr Handle { get; }

    /// <summary>Gets or sets the window title.</summary>
    public string Title
    {
        get => title;
        set
        {
            SDL.SDL_SetWindowTitle(Handle, value);
            title = value;
        }
    }

    /// <summary>Gets the current window width in pixels.</summary>
    public int Width
    {
        get
        {
            SDL.SDL_GetWindowSize(Handle, out var w, out _);
            return w;
        }
    }

    /// <summary>Gets the current window height in pixels.</summary>
    public int Height
    {
        get
        {
            SDL.SDL_GetWindowSize(Handle, out _, out var h);
            return h;
        }
    }

    /// <summary>Gets or sets fullscreen mode.</summary>
    public bool Fullscreen
    {
        get => fullscreen;
        set
        {
            SDL.SDL_SetWindowFullscreen(Handle, value);
            fullscreen = value;
        }
    }

    /// <summary>Gets or sets whether the window is resizable.</summary>
    public bool Resizable
    {
        get => resizable;
        set
        {
            SDL.SDL_SetWindowResizable(Handle, value);
            resizable = value;
        }
    }

    /// <summary>Gets or sets whether the window is borderless.</summary>
    public bool Borderless
    {
        get => borderless;
        set
        {
            SDL.SDL_SetWindowBordered(Handle, !value);
            borderless = value;
        }
    }

    /// <summary>Gets or sets whether the mouse cursor is visible.</summary>
    public bool MouseVisible
    {
        get => mouseVisible;
        set
        {
            SDL.SDL_SetWindowMouseGrab(Handle, !value);
            mouseVisible = value;
        }
    }

    /// <summary>Gets whether the window currently has input focus.</summary>
    public bool IsFocused { get; internal set; }

    /// <summary>Gets whether the window is currently minimized.</summary>
    public bool IsMinimized { get; internal set; }

    /// <summary>Gets whether the window close has been requested (e.g., by clicking the X button).</summary>
    public bool CloseRequested { get; internal set; }

    /// <summary>Gets the GPU rendering backend used by this window.</summary>
    public RenderingBackend Backend { get; }

#endregion

#region Private Fields

    private string title;

    private bool fullscreen;

    private bool resizable;

    private bool borderless;

    private bool mouseVisible;

    private bool disposed;

#endregion

#region Constructor

    /// <summary>
    /// Creates a Window with default settings.
    /// </summary>
    public Window() : this(new())
    {
    }

    /// <summary>
    /// Creates a Window from the specified descriptor.
    /// </summary>
    /// <param name="desc">The window configuration descriptor.</param>
    public Window(WindowDescriptor desc)
    {
        var flags = SDL_WindowFlags.SDL_WINDOW_HIDDEN; // Always create hidden first

        flags |= desc.Fullscreen ?  SDL_WindowFlags.SDL_WINDOW_FULLSCREEN : flags;
        flags |= desc.Resizable ?  SDL_WindowFlags.SDL_WINDOW_RESIZABLE : flags;
        flags |= desc.Borderless ?  SDL_WindowFlags.SDL_WINDOW_BORDERLESS : flags;
        flags |= desc.MouseVisible ?  SDL_WindowFlags.SDL_WINDOW_MOUSE_CAPTURE : flags;
        flags |= desc.HighDpi ?  SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY : flags;
        flags |= desc.AlwaysOnTop ?  SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP : flags;

        switch (desc.Backend)
        {
            case RenderingBackend.Vulkan:
                flags |= SDL_WindowFlags.SDL_WINDOW_VULKAN;
                break;
            case RenderingBackend.D3D12:
                // D3D12 needs no special SDL window flag
                break;
            case RenderingBackend.Metal:
                flags |= SDL_WindowFlags.SDL_WINDOW_METAL;
                break;
        }

        Handle = SDL.SDL_CreateWindow(desc.Title, desc.Width, desc.Height, flags);

        if (Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create SDL3 window: {SDL.SDL_GetError()}");
        }

        title = desc.Title;
        fullscreen = desc.Fullscreen;
        resizable = desc.Resizable;
        borderless = desc.Borderless;
        mouseVisible = desc.MouseVisible;
        Backend = desc.Backend;

        if (!desc.Hidden)
        {
            SDL.SDL_ShowWindow(Handle);
        }
    }

#endregion

#region Public Methods

    /// <summary>Shows the window.</summary>
    public void Show()
    {
        SDL.SDL_ShowWindow(Handle);
    }

    /// <summary>Hides the window.</summary>
    public void Hide()
    {
        SDL.SDL_HideWindow(Handle);
    }

    /// <summary>Maximizes the window.</summary>
    public void Maximize()
    {
        SDL.SDL_MaximizeWindow(Handle);
    }

    /// <summary>Minimizes the window.</summary>
    public void Minimize()
    {
        SDL.SDL_MinimizeWindow(Handle);
    }

    /// <summary>Restores the window from minimized/maximized state.</summary>
    public void Restore()
    {
        SDL.SDL_RestoreWindow(Handle);
    }

#endregion

#region IDisposable Implementation

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;

        if (Handle != IntPtr.Zero)
        {
            SDL.SDL_DestroyWindow(Handle);
        }
    }

#endregion
}
