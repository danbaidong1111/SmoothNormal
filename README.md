<div align="center">
  
  # SmoothNormal
</div>

# Purpose
The purpose of this Unity package is to calculate smooth normals for objects that require outlining. In cartoon rendering, smooth normal information is often needed to achieve a good outlining effect.
# Algorithm
The smooth normal algorithm employs angle-weighted averaging and is accelerated using a `compute shader`. However, it's important to note that the current algorithm has a time complexity of O(n^2). Therefore, I recommend splitting your models based on materials rather than using submeshes to optimize performance.
# Usage
You can add this package by UPM (Unity Packages Manager), url like: `https://github.com/danbaidong1111/SmoothNormal.git#v1.0.1`.

The SmoothNormal package filters objects during model import based on the model's name or import path. When a matching model is imported, smooth normals are calculated and ultimately stored in vertex colors or tangents. Users can create their own custom config file by right-clicking in UnityAsset -> Create -> SmoothNormalGlobalConfig. This user-defined config file is globally unique. The default config file is located in the package directory under the "Runtime" folder, named "SmoothNormalAsset." You can determine the current configuration used by checking the "Use User Config" field here.
