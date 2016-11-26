# float-info
System.Single (float) Next/Prev values, margins for given points, etc

For any given float X (System.Single), this little utility provides
- The least float that is strictly greater than X;
- The greatest float that is strictly less than X;
- The greatest float Y such that (X+Y)==X;

This will perhaps be rewritten using an arbitrary precision arithmetics library,
as the standard ToString() provides questionable results in some cases. 
