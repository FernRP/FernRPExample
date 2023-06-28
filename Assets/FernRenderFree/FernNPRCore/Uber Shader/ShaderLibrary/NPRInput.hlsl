#ifndef UNIVERSAL_NPR_INPUT_INCLUDED
#define UNIVERSAL_NPR_INPUT_INCLUDED

// Must match Universal ShaderGraph master node
struct NPRAddInputData
{
    #if EYE
        half3 corneaNormalWS;
        half3 irisNormalWS;
    #endif

    half linearEyeDepth;
};

#endif
