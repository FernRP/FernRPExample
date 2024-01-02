#ifndef UNIVERSAL_FERN_BSDF_INCLUDED
#define UNIVERSAL_FERN_BSDF_INCLUDED

BRDFData CreateNPRClearCoatBRDFData(FernSurfaceData surfaceData, inout BRDFData brdfData)
{
    BRDFData brdfDataClearCoat = (BRDFData)0;

    #if _CLEARCOAT
        // base brdfData is modified here, rely on the compiler to eliminate dead computation by InitializeBRDFData()
        InitializeBRDFDataClearCoat(surfaceData.clearCoatMask, surfaceData.clearCoatSmoothness, brdfData, brdfDataClearCoat);
    #endif

    return brdfDataClearCoat;
}

void FernInitializeBRDFData(FernSurfaceData surfaceData, out BRDFData outBRDFData, out BRDFData outClearBRDFData)
{
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, outBRDFData);
    outClearBRDFData = outBRDFData;
    #if _CLEARCOAT
    outClearBRDFData = CreateNPRClearCoatBRDFData(surfaceData, outBRDFData);
    #endif
}

#endif
