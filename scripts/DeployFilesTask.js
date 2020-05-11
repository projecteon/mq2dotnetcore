"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
Object.defineProperty(exports, "__esModule", { value: true });
// IMPORTS
var DateHelpers = require("date-fns");
var FileSystemTools = require("fs-extra");
var Klaw = require("klaw");
var OptionParser = require("yargs");
var PathTools = require("path");
// INTERFACES
var FileCopyStatus;
(function (FileCopyStatus) {
    FileCopyStatus[FileCopyStatus["None"] = 0] = "None";
    FileCopyStatus[FileCopyStatus["Copied"] = 1] = "Copied";
    FileCopyStatus[FileCopyStatus["Failed"] = 2] = "Failed";
    FileCopyStatus[FileCopyStatus["SkippedLocked"] = 3] = "SkippedLocked";
    FileCopyStatus[FileCopyStatus["SkippedUnmodified"] = 4] = "SkippedUnmodified";
})(FileCopyStatus || (FileCopyStatus = {}));
var getFormattedErrorMessage = function (errorToFormat) {
    if (!errorToFormat) {
        return errorToFormat;
    }
    return errorToFormat.stack || errorToFormat.toString();
};
var normalizePathInput = function (pathToNormalize) {
    if (!pathToNormalize) {
        return pathToNormalize;
    }
    return pathToNormalize.trim().replace(/\\$/, '').replace(/\/$/, '');
    ;
};
var walkFilesAsync = function (sourceDirectory, excludeDirectories, options) {
    return new Promise(function (resolve, reject) {
        var items = [];
        Klaw(sourceDirectory, options)
            .on('data', function (nextFileItem) {
            if (!excludeDirectories || !nextFileItem.stats.isDirectory()) {
                items.push(nextFileItem);
            }
        })
            .on('end', function () { return resolve(items); })
            .on('error', reject);
    });
};
var DATE_TIME_FORMAT = 'yyyy-MM-dd  HH:mm:ss.SSSxxx';
var isNullOrUndefined = function (value) {
    return value === null || value === undefined;
};
var destinationPath = OptionParser.argv.destinationPath || undefined;
var sourcePath = OptionParser.argv.sourcePath || undefined;
// Purges any files in destination directory that were not found in the source directory
var purgeAfterCopy = false;
if (!isNullOrUndefined(OptionParser.argv.purge)) {
    purgeAfterCopy = Boolean(OptionParser.argv.purge);
}
var renameLockedFiles = true;
if (!isNullOrUndefined(OptionParser.argv.rename)) {
    renameLockedFiles = Boolean(OptionParser.argv.rename);
}
var skipUnmodified = true;
if (!isNullOrUndefined(OptionParser.argv.skipUnmodified)) {
    skipUnmodified = Boolean(OptionParser.argv.skipUnmodified);
}
console.log(OptionParser.argv);
var hasRequiredParameters = true;
if (!sourcePath) {
    hasRequiredParameters = false;
    console.error('--sourcePath <path> is required');
}
if (!destinationPath) {
    hasRequiredParameters = false;
    console.error('--destinationPath <path> is required');
}
if (!hasRequiredParameters) {
    process.exit(1);
}
var DeployFilesTask = /** @class */ (function () {
    function DeployFilesTask(destinationPath, isPurgeAfterCopyEnabled, isRenameAnyLockedFilesEnabled, isSkipUnmodifiedEnabled, sourcePath) {
        var _this = this;
        this.runAsync = function () { return __awaiter(_this, void 0, void 0, function () {
            var doesSurcePathExist, startTime, formattedStartTime, sourceFileItems, destinationDirectory, destinationDirectoryAlreadyExists, sourcePathInformation, isSingleFileCopy, destinationFileItems, removedOldFileCount, removedOldFileFailureCount, _i, destinationFileItems_1, nextFileItem, oldFilePathToRemove, removeFileError_1, copyFilePromises, _a, sourceFileItems_1, nextFileItem, fileCopyResults, renamedFileCount, unknownStatusResults, successResults, failedResults, skippedLockedResults, skippedUnmodifiedResults, _b, fileCopyResults_1, nextFileCopyResult, wasSuccesful, runPurgeAfterCopy, destinationFileItems, purgedFileSuccessCount, purgedFileFailedCount, _loop_1, this_1, _c, destinationFileItems_2, nextDestinationFileItem, endTime, formattedEndTime, ellapsedMilliseconds, taskError_1;
            return __generator(this, function (_d) {
                switch (_d.label) {
                    case 0:
                        _d.trys.push([0, 22, , 23]);
                        return [4 /*yield*/, FileSystemTools.pathExists(this.sourcePath)];
                    case 1:
                        doesSurcePathExist = _d.sent();
                        if (!doesSurcePathExist) {
                            throw new Error("The source path does not exist: " + this.sourcePath);
                        }
                        startTime = new Date();
                        formattedStartTime = DateHelpers.format(startTime, DATE_TIME_FORMAT);
                        console.log("DeployFilesTask  - Start Time: " + formattedStartTime);
                        sourceFileItems = void 0;
                        destinationDirectory = this.destinationPath;
                        destinationDirectoryAlreadyExists = false;
                        return [4 /*yield*/, FileSystemTools.stat(this.sourcePath)];
                    case 2:
                        sourcePathInformation = _d.sent();
                        isSingleFileCopy = !sourcePathInformation.isDirectory();
                        if (!isSingleFileCopy) return [3 /*break*/, 3];
                        console.debug('Performing single file copy');
                        sourceFileItems = [{
                                path: this.sourcePath,
                                stats: sourcePathInformation
                            }];
                        destinationDirectory = PathTools.dirname(destinationDirectory);
                        return [3 /*break*/, 5];
                    case 3:
                        // Get source files, excluding directories
                        console.debug("Walking source file directory...");
                        return [4 /*yield*/, walkFilesAsync(this.sourcePath, true)];
                    case 4:
                        sourceFileItems = _d.sent();
                        if (!sourceFileItems || !sourceFileItems.length) {
                            console.warn("No source files were located! Exiting task.");
                            return [2 /*return*/];
                        }
                        _d.label = 5;
                    case 5: return [4 /*yield*/, FileSystemTools.existsSync(destinationDirectory)];
                    case 6:
                        // Check now if the destination directory already exists
                        destinationDirectoryAlreadyExists = _d.sent();
                        if (!(this.isRenameAnyLockedFilesEnabled && destinationDirectoryAlreadyExists)) return [3 /*break*/, 14];
                        console.debug('\nInspecting the destination directory for any .old files that can be removed...');
                        return [4 /*yield*/, walkFilesAsync(destinationDirectory, true)];
                    case 7:
                        destinationFileItems = _d.sent();
                        removedOldFileCount = 0;
                        removedOldFileFailureCount = 0;
                        _i = 0, destinationFileItems_1 = destinationFileItems;
                        _d.label = 8;
                    case 8:
                        if (!(_i < destinationFileItems_1.length)) return [3 /*break*/, 13];
                        nextFileItem = destinationFileItems_1[_i];
                        oldFilePathToRemove = nextFileItem.path;
                        if (!(/\.old\d*/.test(oldFilePathToRemove))) {
                            // ignore it if it's not a .old file
                            return [3 /*break*/, 12];
                        }
                        _d.label = 9;
                    case 9:
                        _d.trys.push([9, 11, , 12]);
                        //console.debug(`Attempting to remove old file: ${oldFilePathToRemove}`);
                        return [4 /*yield*/, FileSystemTools.unlink(oldFilePathToRemove)];
                    case 10:
                        //console.debug(`Attempting to remove old file: ${oldFilePathToRemove}`);
                        _d.sent();
                        ++removedOldFileCount;
                        return [3 /*break*/, 12];
                    case 11:
                        removeFileError_1 = _d.sent();
                        //console.debug(`Failed to remove old file: ${oldFilePathToRemove}`);
                        ++removedOldFileFailureCount;
                        return [3 /*break*/, 12];
                    case 12:
                        _i++;
                        return [3 /*break*/, 8];
                    case 13:
                        if (removedOldFileCount > 0) {
                            console.log("Removed .old files count: " + removedOldFileCount);
                        }
                        if (removedOldFileFailureCount > 0) {
                            console.warn("Unable to remove " + removedOldFileFailureCount + " .old files.");
                        }
                        _d.label = 14;
                    case 14:
                        console.debug("Copying source files to destination directory...");
                        copyFilePromises = [];
                        for (_a = 0, sourceFileItems_1 = sourceFileItems; _a < sourceFileItems_1.length; _a++) {
                            nextFileItem = sourceFileItems_1[_a];
                            if (!nextFileItem || !nextFileItem.path || !nextFileItem.stats) {
                                continue;
                            }
                            copyFilePromises.push(this.doFileCopyAsync(nextFileItem, destinationDirectory, this.isRenameAnyLockedFilesEnabled));
                        }
                        console.debug("Awaiting all copy file promises...");
                        return [4 /*yield*/, Promise.all(copyFilePromises)];
                    case 15:
                        fileCopyResults = _d.sent();
                        renamedFileCount = 0;
                        unknownStatusResults = [];
                        successResults = [];
                        failedResults = [];
                        skippedLockedResults = [];
                        skippedUnmodifiedResults = [];
                        for (_b = 0, fileCopyResults_1 = fileCopyResults; _b < fileCopyResults_1.length; _b++) {
                            nextFileCopyResult = fileCopyResults_1[_b];
                            if (!nextFileCopyResult) {
                                console.debug('Warning: nextFileCopyResult is null/undefined');
                                continue;
                            }
                            switch (nextFileCopyResult.Status) {
                                case FileCopyStatus.Copied:
                                    if (nextFileCopyResult.PreviousFileRenamePath) {
                                        ++renamedFileCount;
                                    }
                                    successResults.push(nextFileCopyResult);
                                    break;
                                case FileCopyStatus.Failed:
                                    //console.debug(`Failed To Copy: ${nextFileCopyResult.SourcePath}`);
                                    failedResults.push(nextFileCopyResult);
                                    break;
                                case FileCopyStatus.SkippedLocked:
                                    //console.debug(`Skipped Locked/Busy File: ${nextFileCopyResult.SourcePath}`);
                                    skippedLockedResults.push(nextFileCopyResult);
                                    break;
                                case FileCopyStatus.SkippedUnmodified:
                                    skippedUnmodifiedResults.push(nextFileCopyResult);
                                    break;
                                default:
                                    //console.debug(`Unknown Copy Status: ${nextFileCopyResult.SourcePath}`);
                                    unknownStatusResults.push(nextFileCopyResult);
                                    break;
                            }
                        }
                        wasSuccesful = true;
                        console.log("\nCopied:                  " + successResults.length);
                        if (this.isSkipUnmodifiedEnabled) {
                            console.log("Skipped (unmodified):   " + skippedUnmodifiedResults.length);
                        }
                        if (renamedFileCount > 0) {
                            console.log("Renamed Locked Files:   " + renamedFileCount);
                        }
                        if (failedResults.length > 0) {
                            console.warn("Failed:                   " + failedResults.length);
                            wasSuccesful = false;
                        }
                        if (skippedLockedResults.length > 0) {
                            console.warn("Skipped (locked):         " + skippedLockedResults.length);
                            wasSuccesful = false;
                        }
                        if (unknownStatusResults.length > 0) {
                            console.warn("Unknown Status:           " + unknownStatusResults.length);
                            wasSuccesful = false;
                        }
                        runPurgeAfterCopy = wasSuccesful
                            && this.isPurgeAfterCopyEnabled
                            && !isSingleFileCopy
                            && destinationDirectoryAlreadyExists;
                        if (!runPurgeAfterCopy) return [3 /*break*/, 21];
                        console.debug('Walking destination files needed for purge comparison...');
                        return [4 /*yield*/, walkFilesAsync(destinationDirectory, true)];
                    case 16:
                        destinationFileItems = _d.sent();
                        console.log('\nPurging destination files that we\'re not part of the source files');
                        purgedFileSuccessCount = 0;
                        purgedFileFailedCount = 0;
                        if (!(destinationFileItems && destinationFileItems.length > 0)) return [3 /*break*/, 20];
                        _loop_1 = function (nextDestinationFileItem) {
                            var destinationFilePath, relativeDestinationPath, absoluteSourcePath, purgeError_1;
                            return __generator(this, function (_a) {
                                switch (_a.label) {
                                    case 0:
                                        destinationFilePath = nextDestinationFileItem.path;
                                        relativeDestinationPath = PathTools.relative(destinationDirectory, destinationFilePath);
                                        absoluteSourcePath = PathTools.resolve(this_1.sourcePath, relativeDestinationPath);
                                        if (sourceFileItems.some(function (sourceFileItem) { return sourceFileItem.path === absoluteSourcePath || sourceFileItem.path === relativeDestinationPath; })) {
                                            return [2 /*return*/, "continue"];
                                        }
                                        _a.label = 1;
                                    case 1:
                                        _a.trys.push([1, 3, , 4]);
                                        //console.debug(`Attempting to purge destination file: ${destinationFilePath}`);
                                        return [4 /*yield*/, FileSystemTools.remove(destinationFilePath)];
                                    case 2:
                                        //console.debug(`Attempting to purge destination file: ${destinationFilePath}`);
                                        _a.sent();
                                        ++purgedFileSuccessCount;
                                        return [3 /*break*/, 4];
                                    case 3:
                                        purgeError_1 = _a.sent();
                                        console.warn("Unable to purge: " + destinationFilePath);
                                        ++purgedFileFailedCount;
                                        return [3 /*break*/, 4];
                                    case 4: return [2 /*return*/];
                                }
                            });
                        };
                        this_1 = this;
                        _c = 0, destinationFileItems_2 = destinationFileItems;
                        _d.label = 17;
                    case 17:
                        if (!(_c < destinationFileItems_2.length)) return [3 /*break*/, 20];
                        nextDestinationFileItem = destinationFileItems_2[_c];
                        return [5 /*yield**/, _loop_1(nextDestinationFileItem)];
                    case 18:
                        _d.sent();
                        _d.label = 19;
                    case 19:
                        _c++;
                        return [3 /*break*/, 17];
                    case 20:
                        console.log("Purged:                  " + purgedFileSuccessCount);
                        if (purgedFileFailedCount > 0) {
                            console.warn("Unable to purge:         " + purgedFileFailedCount);
                        }
                        _d.label = 21;
                    case 21:
                        endTime = new Date();
                        formattedEndTime = DateHelpers.format(endTime, DATE_TIME_FORMAT);
                        ellapsedMilliseconds = endTime.getTime() - startTime.getTime();
                        console.log("\nEnd Time:  " + formattedEndTime + "  [Ellapased: " + ellapsedMilliseconds + "ms]");
                        return [3 /*break*/, 23];
                    case 22:
                        taskError_1 = _d.sent();
                        console.error(taskError_1);
                        return [3 /*break*/, 23];
                    case 23:
                        console.debug("Exiting DeployFilesTask.runAsync().");
                        return [2 /*return*/];
                }
            });
        }); };
        this.destinationPath = destinationPath;
        this.isPurgeAfterCopyEnabled = isPurgeAfterCopyEnabled;
        this.isRenameAnyLockedFilesEnabled = isRenameAnyLockedFilesEnabled;
        this.isSkipUnmodifiedEnabled = isSkipUnmodifiedEnabled;
        this.sourcePath = sourcePath;
    }
    DeployFilesTask.prototype.doFileCopyAsync = function (sourceItem, destinationDirectory, isRenameAndReplacedEnabled) {
        return __awaiter(this, void 0, void 0, function () {
            var sourceFilePath, absoluteDestinationPath, relativeSourcePath, destinationFileStats, isUnmodified, absoluteDestinationDirectory, asyncError_1, shouldTryRenaming, previousFileRenamePath;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        _a.trys.push([0, 2, , 5]);
                        sourceFilePath = sourceItem.path;
                        relativeSourcePath = PathTools.relative(this.sourcePath, sourceFilePath);
                        absoluteDestinationPath = PathTools.resolve(destinationDirectory, relativeSourcePath);
                        if (this.isSkipUnmodifiedEnabled) {
                            try {
                                destinationFileStats = FileSystemTools.statSync(absoluteDestinationPath);
                                isUnmodified = destinationFileStats
                                    && destinationFileStats.size === sourceItem.stats.size
                                    && destinationFileStats.mtimeMs === sourceItem.stats.mtimeMs;
                                if (isUnmodified) {
                                    return [2 /*return*/, {
                                            DestinationPath: absoluteDestinationPath,
                                            SourcePath: sourceFilePath,
                                            Status: FileCopyStatus.SkippedUnmodified
                                        }];
                                }
                            }
                            catch (getStatsError) {
                                console.debug(getFormattedErrorMessage(getStatsError));
                            }
                        }
                        absoluteDestinationDirectory = PathTools.dirname(absoluteDestinationPath);
                        try {
                            FileSystemTools.ensureDirSync(absoluteDestinationDirectory);
                        }
                        catch (ensureDirectoriesError) {
                            if (ensureDirectoriesError && ensureDirectoriesError.code && ensureDirectoriesError.code === 'EEXIST') {
                                // ensure directory is not supposed to error if the directory already exists, but it is doing so for some reason...
                                // seems like it does so when there are permissions errors, so we'll skip this error. If it's a permissions
                                // error the copyFile(..) call should throw with the correct EPERM error code so we can see what is actually
                                // causing the problem
                                console.debug('ensureDirSync(..) returned an already exists error for: ' + absoluteDestinationPath);
                                console.debug(ensureDirectoriesError);
                                console.debug('Continuing to the copyFile(..) step');
                            }
                            else {
                                throw ensureDirectoriesError;
                            }
                        }
                        return [4 /*yield*/, FileSystemTools.copyFile(sourceFilePath, absoluteDestinationPath)];
                    case 1:
                        _a.sent();
                        return [2 /*return*/, {
                                DestinationPath: absoluteDestinationPath,
                                SourcePath: sourceFilePath,
                                Status: FileCopyStatus.Copied
                            }];
                    case 2:
                        asyncError_1 = _a.sent();
                        shouldTryRenaming = isRenameAndReplacedEnabled
                            && asyncError_1
                            && asyncError_1.code
                            && asyncError_1.code === 'EBUSY';
                        if (!shouldTryRenaming || !sourceFilePath || !absoluteDestinationPath) {
                            throw asyncError_1;
                        }
                        return [4 /*yield*/, this.renameLockedFileAsync(absoluteDestinationPath)];
                    case 3:
                        previousFileRenamePath = _a.sent();
                        if (!previousFileRenamePath) {
                            // Failed to rename
                            return [2 /*return*/, {
                                    DestinationPath: absoluteDestinationPath,
                                    SourcePath: sourceFilePath,
                                    Status: FileCopyStatus.SkippedLocked
                                }];
                        }
                        // Try the copy again now that we have renamed the old file
                        return [4 /*yield*/, FileSystemTools.copyFile(sourceFilePath, absoluteDestinationPath)];
                    case 4:
                        // Try the copy again now that we have renamed the old file
                        _a.sent();
                        return [2 /*return*/, {
                                DestinationPath: absoluteDestinationPath,
                                PreviousFileRenamePath: previousFileRenamePath,
                                SourcePath: sourceFilePath,
                                Status: FileCopyStatus.Copied
                            }];
                    case 5: return [2 /*return*/];
                }
            });
        });
    };
    DeployFilesTask.prototype.renameLockedFileAsync = function (destinationPath) {
        return __awaiter(this, void 0, void 0, function () {
            var newNameIndex, previousFileRenamePath, asyncError_2;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        _a.trys.push([0, 2, , 3]);
                        newNameIndex = 1;
                        previousFileRenamePath = destinationPath + ".old" + newNameIndex;
                        while (FileSystemTools.existsSync(previousFileRenamePath)) {
                            ++newNameIndex;
                            previousFileRenamePath = destinationPath + ".old" + newNameIndex;
                        }
                        //console.debug(`Renaming locked file ${destinationPath} to:  ${previousFileRenamePath}`);
                        return [4 /*yield*/, FileSystemTools.move(destinationPath, previousFileRenamePath)];
                    case 1:
                        //console.debug(`Renaming locked file ${destinationPath} to:  ${previousFileRenamePath}`);
                        _a.sent();
                        return [2 /*return*/, previousFileRenamePath];
                    case 2:
                        asyncError_2 = _a.sent();
                        console.warn("ERROR: Failed to rename:  " + destinationPath + "\n");
                        console.warn(getFormattedErrorMessage(asyncError_2));
                        return [2 /*return*/, undefined];
                    case 3: return [2 /*return*/];
                }
            });
        });
    };
    return DeployFilesTask;
}());
// Ok to cast these as string, we validate they're defined above
var deployFilesTask = new DeployFilesTask(destinationPath, purgeAfterCopy, renameLockedFiles, skipUnmodified, sourcePath);
deployFilesTask.runAsync()
    .then(function () {
    process.exit(0);
})
    .catch(function (asyncError) {
    console.error('(DeployFilesTask) ERROR: ');
    console.error(getFormattedErrorMessage(asyncError));
    process.exit(1);
});
