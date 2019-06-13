#!/usr/bin/python

from __future__ import print_function
import os
import sys
import urllib2

BAD_WORDS = "https://www.cs.cmu.edu/~biglou/resources/bad-words.txt"

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Provide file to clean as sole argument\n\n")
        sys.exit(-1)

    badWords = urllib2.urlopen(BAD_WORDS).read().split("\n")
    inPath = os.path.abspath(sys.argv[1])
    outPath = "{}.cleaned".format(inPath)

    (inCount, outCount) = (0, 0)
    with open(inPath, "r") as inFile:
        with open(outPath, "w+") as outFile:
            for inLine in inFile:
                inCount += 1
                if inLine.strip().lower() not in badWords:
                    outCount += 1
                    outFile.write(inLine)
            outFile.close()
        inFile.close()
    
    print("Removed {} words from {}.\nClean file saved to {}".format(
        inCount - outCount, os.path.basename(inPath), outPath))