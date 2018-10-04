This scene shows a very basic "blurry refraction" technique using
command buffers.

The idea is: after opaque objects & skybox is rendered, copy the image
into a temporary render target, blur it and set up a global shader property
with the result. Objects that are rendered after skybox (i.e. all
semitransparent objects) can then sample this "blurred scene image". This is
like GrabPass, just a bit better!

See CommandBufferBlurRefraction.cs script that is attached to the glass object
in the scene.

Caveat: right now it does not capture the scene view properly though; the
texture ends up only containing the skybox :(

Caveat: this is small example code, and not a production-ready refractive
blurred glass system. It very likely won't deal with multiple objects having
the script attached to them, etc.
