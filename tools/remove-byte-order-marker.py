import sys
import os
import codecs

scriptLocation = os.path.dirname(os.path.realpath(sys.argv[0]))
repositoryRoot = os.path.dirname(scriptLocation)

retv = 0

for changedFile in sys.argv[1:]:
    # file pathes from command line args are relative to the repository root
    changedFile = os.path.realpath(os.path.join(repositoryRoot, changedFile))

    BUFSIZE = 4096
    with open(changedFile, "r+b") as fp:
        bytesToRemove = 0
        chunk = fp.read(5)
        # utf8 byte order marker EF BB BF
        if chunk.startswith(codecs.BOM_UTF8):
            bytesToRemove = 3
            print(f"{changedFile}: Has a utf-8 byte-order marker")
        # Python automatically detects endianess if utf-16 bom is present
        # write endianess generally determined by endianess of CPU
        elif chunk.startswith(b"\xfe\xff") or chunk.startswith(b"\xff\xfe"):
            bytesToRemove = 2
            print(f"{changedFile}: Has a utf-16 byte-order marker")

        elif chunk.startswith(b"\xfe\xff\x00\x00") or chunk.startswith(b"\x00\x00\xff\xfe"):
            bytesToRemove = 5
            print(f"{changedFile}: Has a utf-32 byte-order marker")

        if bytesToRemove > 0:
            retv = 1
            i = 0
            chunk = chunk[bytesToRemove:]
            while chunk:
                fp.seek(i)
                fp.write(chunk)
                i += len(chunk)
                fp.seek(bytesToRemove, os.SEEK_CUR)
                chunk = fp.read(BUFSIZE)
            fp.seek(-bytesToRemove, os.SEEK_CUR)
            fp.truncate()

exit(retv)
