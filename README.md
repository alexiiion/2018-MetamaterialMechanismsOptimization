# 2018-MetamaterialMechanismsOptimization

## ShearCell_Optimization (C++)
Simulation in C++, based on IPOPT (windows binary and build file is included)

## ShearCell_Interaction (C#)
Contains 
- GUI
- Simulated annealing.
- All data management of the cell grid
- Routines to create data sets (currently exported as images)

#### Issues:
Runs only in Release mode now, very likely due to IPOPT/Intel version issues. IPOPT doesnt find the Intel libs (v2019) at runtime, although it works with v2018.

``Intel MKL FATAL ERROR: Cannot load mkl_avx512.dll or mkl_def.dll.``
