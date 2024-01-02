#ifndef UNIVERSAL_FERNLITTING_INPUT_INCLUDED
#define UNIVERSAL_FERNLITTING_INPUT_INCLUDED

// Must match Universal ShaderGraph master node
struct FernAddInputData
{
    #if EYE
        half3 corneaNormalWS;
        half3 irisNormalWS;
    #endif

    half linearEyeDepth;
};

#endif //UNIVERSAL_FERN_INPUT_INCLUDED
