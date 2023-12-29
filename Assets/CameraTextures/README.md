# Camera Textures
The Unity `Camera` component has the property `Target Texture`. As described in the [API](https://docs.unity3d.com/ScriptReference/Camera-targetTexture.html), when this is left to null it will render to the screen, while if a texture is provided it will render to the given texture.

As the UGV's cameras are being sent over ROS and not rendered to a screen, each camera on the vehicle will require a separate render texture.

## Author
Timothy Mead (timothy.mead20@gmail.com)