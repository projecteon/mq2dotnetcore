
// IMPORTS
import DateHelpers = require('date-fns');
import FileSystemTools = require('fs-extra');
import Klaw = require('klaw');
import OptionParser = require('yargs');
import PathTools = require('path');



// INTERFACES
enum FileCopyStatus {
	None,
	Copied,
	Failed,
	SkippedLocked,
	SkippedUnmodified
}

interface IFileCopyResult {
	CopyError?: Error | undefined;
	DestinationPath: string;
	PreviousFileRenamePath?: string | undefined;
	SourcePath: string;
	Status: FileCopyStatus;
}

const getFormattedErrorMessage = (errorToFormat: Error): string | null | undefined => {
	if (!errorToFormat) {
		return errorToFormat as (null | undefined);
	}

	return errorToFormat.stack || errorToFormat.toString();
};

const normalizePathInput = (pathToNormalize: string): string => {
	if (!pathToNormalize) {
		return pathToNormalize;
	}

	return pathToNormalize.trim().replace(/\\$/, '').replace(/\/$/, '');;
};

const walkFilesAsync = (sourceDirectory: string, excludeDirectories: boolean, options?: Klaw.Options): Promise<Klaw.Item[]> => {
	return new Promise((resolve, reject) => {
		const items: Klaw.Item[] = [];
		Klaw(sourceDirectory, options)
			.on('data', (nextFileItem: Klaw.Item) => {
				if (!excludeDirectories || !nextFileItem.stats.isDirectory()) {
					items.push(nextFileItem);
				}
			})
			.on('end', () => resolve(items))
			.on('error', reject);
	});
};

const DATE_TIME_FORMAT = 'yyyy-MM-dd  HH:mm:ss.SSSxxx';

const isNullOrUndefined = (value: unknown): value is null | undefined => {
	return value === null || value === undefined;
}

const destinationPath = (OptionParser.argv.destinationPath as string) || undefined;
const sourcePath = (OptionParser.argv.sourcePath as string) || undefined;

// Purges any files in destination directory that were not found in the source directory
let purgeAfterCopy = false;
if (!isNullOrUndefined(OptionParser.argv.purge)) {
	purgeAfterCopy = Boolean(OptionParser.argv.purge);
}

let renameLockedFiles = true;
if (!isNullOrUndefined(OptionParser.argv.rename)) {
	renameLockedFiles = Boolean(OptionParser.argv.rename);
}

let skipUnmodified = true
if (!isNullOrUndefined(OptionParser.argv.skipUnmodified)) {
	skipUnmodified = Boolean(OptionParser.argv.skipUnmodified);
}

console.log(OptionParser.argv)

let hasRequiredParameters = true;
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



class DeployFilesTask {
	private readonly destinationPath: string;
	private readonly isPurgeAfterCopyEnabled: boolean;
	private readonly isRenameAnyLockedFilesEnabled: boolean;
	private readonly isSkipUnmodifiedEnabled: boolean;
	private readonly sourcePath: string;

	constructor(
		destinationPath: string,
		isPurgeAfterCopyEnabled: boolean,
		isRenameAnyLockedFilesEnabled: boolean,
		isSkipUnmodifiedEnabled: boolean,
		sourcePath: string
	)
	{
		this.destinationPath = destinationPath;
		this.isPurgeAfterCopyEnabled = isPurgeAfterCopyEnabled;
		this.isRenameAnyLockedFilesEnabled = isRenameAnyLockedFilesEnabled;
		this.isSkipUnmodifiedEnabled = isSkipUnmodifiedEnabled;
		this.sourcePath = sourcePath;
	}

	private async doFileCopyAsync(sourceItem: Klaw.Item, destinationDirectory: string, isRenameAndReplacedEnabled: boolean): Promise<IFileCopyResult> {
		let sourceFilePath;
		let absoluteDestinationPath;
		try {
			sourceFilePath = sourceItem.path;
			const relativeSourcePath = PathTools.relative(this.sourcePath, sourceFilePath);
			absoluteDestinationPath = PathTools.resolve(destinationDirectory, relativeSourcePath);

			if (this.isSkipUnmodifiedEnabled) {
				try {
					if (FileSystemTools.existsSync(absoluteDestinationPath)) {

						const destinationFileStats = FileSystemTools.statSync(absoluteDestinationPath);

						const isUnmodified = destinationFileStats
							&& destinationFileStats.size === sourceItem.stats.size
							&& destinationFileStats.mtimeMs === sourceItem.stats.mtimeMs

						if (isUnmodified) {
							return {
								DestinationPath: absoluteDestinationPath,
								SourcePath: sourceFilePath,
								Status: FileCopyStatus.SkippedUnmodified
							};
						}
					}
				} catch (getStatsError) {
					console.debug(getFormattedErrorMessage(getStatsError));
				}
			}

			const absoluteDestinationDirectory = PathTools.dirname(absoluteDestinationPath);
			try {
				FileSystemTools.ensureDirSync(absoluteDestinationDirectory);
			} catch (ensureDirectoriesError) {
				if (ensureDirectoriesError && ensureDirectoriesError.code && ensureDirectoriesError.code === 'EEXIST') {
					// ensure directory is not supposed to error if the directory already exists, but it is doing so for some reason...
					// seems like it does so when there are permissions errors, so we'll skip this error. If it's a permissions
					// error the copyFile(..) call should throw with the correct EPERM error code so we can see what is actually
					// causing the problem
					console.debug('ensureDirSync(..) returned an already exists error for: ' + absoluteDestinationPath);
					console.debug(ensureDirectoriesError);
					console.debug('Continuing to the copyFile(..) step');
				} else {
					throw ensureDirectoriesError;
				}
			}

			await FileSystemTools.copyFile(sourceFilePath, absoluteDestinationPath);

			return {
				DestinationPath: absoluteDestinationPath,
				SourcePath: sourceFilePath,
				Status: FileCopyStatus.Copied
			};
		} catch (asyncError) {
			const shouldTryRenaming = isRenameAndReplacedEnabled
				&& asyncError
				&& asyncError.code
				&& asyncError.code === 'EBUSY';

			if (!shouldTryRenaming || !sourceFilePath || !absoluteDestinationPath) {
				throw asyncError;
			}

			const previousFileRenamePath = await this.renameLockedFileAsync(absoluteDestinationPath);
			if (!previousFileRenamePath) {
				// Failed to rename
				return {
					DestinationPath: absoluteDestinationPath,
					SourcePath: sourceFilePath,
					Status: FileCopyStatus.SkippedLocked
				}
			}

			// Try the copy again now that we have renamed the old file
			await FileSystemTools.copyFile(sourceFilePath, absoluteDestinationPath);

			return {
				DestinationPath: absoluteDestinationPath,
				PreviousFileRenamePath: previousFileRenamePath,
				SourcePath: sourceFilePath,
				Status: FileCopyStatus.Copied
			};
		}
	}

	private async renameLockedFileAsync(destinationPath: string): Promise<string | undefined> {
		try {
			let newNameIndex = 1;
			let previousFileRenamePath = `${destinationPath}.old${newNameIndex}`;
			while (FileSystemTools.existsSync(previousFileRenamePath)) {
				++newNameIndex;
				previousFileRenamePath = `${destinationPath}.old${newNameIndex}`;
			}

			//console.debug(`Renaming locked file ${destinationPath} to:  ${previousFileRenamePath}`);
			await FileSystemTools.move(destinationPath, previousFileRenamePath);
			return previousFileRenamePath;
		} catch (asyncError) {
			console.warn(`ERROR: Failed to rename:  ${destinationPath}\n`);
			console.warn(getFormattedErrorMessage(asyncError));
			return undefined;
		}
	}

	public runAsync = async (): Promise<void> => {
		try {

			const doesSurcePathExist = await FileSystemTools.pathExists(this.sourcePath);
			if (!doesSurcePathExist) {
				throw new Error(`The source path does not exist: ${this.sourcePath}`);
			}

			//if (!this.isRenameAndReplacedAnyLockedFilesEnabled) {
			// TODO: We could determine if robocopy is available on the machine and use it here if we wanted
			// for a potential performance boost. I'm not sure if/how much it would matter for a local copy
			// but it's something to consider...
			//}

			const startTime = new Date();
			const formattedStartTime = DateHelpers.format(startTime, DATE_TIME_FORMAT);
			console.log(`DeployFilesTask  - Start Time: ${formattedStartTime}`);

			let sourceFileItems: Klaw.Item[];
			let destinationDirectory = this.destinationPath;
			let destinationDirectoryAlreadyExists = false;

			const sourcePathInformation = await FileSystemTools.stat(this.sourcePath);
			const isSingleFileCopy = !sourcePathInformation.isDirectory();
			if (isSingleFileCopy) {
				console.debug('Performing single file copy');

				sourceFileItems = [{
					path: this.sourcePath,
					stats: sourcePathInformation
				}];

				destinationDirectory = PathTools.dirname(destinationDirectory);
			} else {

				// Get source files, excluding directories
				console.debug("Walking source file directory...");
				sourceFileItems = await walkFilesAsync(this.sourcePath, true);
				if (!sourceFileItems || !sourceFileItems.length) {
					console.warn(`No source files were located! Exiting task.`);
					return;
				}
			}

			// Check now if the destination directory already exists
			destinationDirectoryAlreadyExists = await FileSystemTools.existsSync(destinationDirectory);

			if (this.isRenameAnyLockedFilesEnabled && destinationDirectoryAlreadyExists) {
				console.debug('\nInspecting the destination directory for any .old files that can be removed...');
				const destinationFileItems = await walkFilesAsync(destinationDirectory, true);
				let removedOldFileCount = 0;
				let removedOldFileFailureCount = 0;
				for (const nextFileItem of destinationFileItems) {
					const oldFilePathToRemove = nextFileItem.path;

					if (!(/\.old\d*/.test(oldFilePathToRemove))) {
						// ignore it if it's not a .old file
						continue;
					}

					try {
						//console.debug(`Attempting to remove old file: ${oldFilePathToRemove}`);
						await FileSystemTools.unlink(oldFilePathToRemove);
						++removedOldFileCount;
					} catch (removeFileError) {
						//console.debug(`Failed to remove old file: ${oldFilePathToRemove}`);
						++removedOldFileFailureCount;
					}
				}

				if (removedOldFileCount > 0) {
					console.log(`Removed .old files count: ${removedOldFileCount}`);
				}

				if (removedOldFileFailureCount > 0) {
					console.warn(`Unable to remove ${removedOldFileFailureCount} .old files.`);
				}
			}

			console.debug("Copying source files to destination directory...");
			const copyFilePromises: Promise<any>[] = [];
			for (const nextFileItem of sourceFileItems) {
				if (!nextFileItem || !nextFileItem.path || !nextFileItem.stats) {
					continue;
				}

				copyFilePromises.push(this.doFileCopyAsync(nextFileItem, destinationDirectory, this.isRenameAnyLockedFilesEnabled));
			}

			console.debug("Awaiting all copy file promises...");
			const fileCopyResults = await Promise.all(copyFilePromises);

			let renamedFileCount = 0;
			let unknownStatusResults: IFileCopyResult[] = [];
			let successResults: IFileCopyResult[] = [];
			let failedResults: IFileCopyResult[] = [];
			let skippedLockedResults: IFileCopyResult[] = [];
			let skippedUnmodifiedResults: IFileCopyResult[] = [];
			for (let nextFileCopyResult of fileCopyResults) {
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

			let wasSuccesful = true;
			console.log(`\nCopied:                  ${successResults.length}`);

			if (this.isSkipUnmodifiedEnabled) {
				console.log(`Skipped (unmodified):   ${skippedUnmodifiedResults.length}`);
			}

			if (renamedFileCount > 0) {
				console.log(`Renamed Locked Files:   ${renamedFileCount}`);
			}

			if (failedResults.length > 0) {
				console.warn(`Failed:                   ${failedResults.length}`);
				wasSuccesful = false;
			}

			if (skippedLockedResults.length > 0) {
				console.warn(`Skipped (locked):         ${skippedLockedResults.length}`);
				wasSuccesful = false;
			}

			if (unknownStatusResults.length > 0) {
				console.warn(`Unknown Status:           ${unknownStatusResults.length}`);
				wasSuccesful = false;
			}

			const runPurgeAfterCopy =
				wasSuccesful
				&& this.isPurgeAfterCopyEnabled
				&& !isSingleFileCopy
				&& destinationDirectoryAlreadyExists;

			if (runPurgeAfterCopy) {
				console.debug('Walking destination files needed for purge comparison...');
				const destinationFileItems = await walkFilesAsync(destinationDirectory, true);

				console.log('\nPurging destination files that we\'re not part of the source files');
				let purgedFileSuccessCount = 0;
				let purgedFileFailedCount = 0;
				if (destinationFileItems && destinationFileItems.length > 0) {
					for (const nextDestinationFileItem of destinationFileItems) {
						const destinationFilePath = nextDestinationFileItem.path;
						const relativeDestinationPath = PathTools.relative(destinationDirectory, destinationFilePath);
						const absoluteSourcePath = PathTools.resolve(this.sourcePath, relativeDestinationPath);

						if (sourceFileItems.some(sourceFileItem => sourceFileItem.path === absoluteSourcePath || sourceFileItem.path === relativeDestinationPath)) {
							// destination file has matching source file
							continue;
						}

						try {
							//console.debug(`Attempting to purge destination file: ${destinationFilePath}`);
							await FileSystemTools.remove(destinationFilePath);
							++purgedFileSuccessCount;
						} catch (purgeError) {
							console.warn(`Unable to purge: ${destinationFilePath}`);
							++purgedFileFailedCount;
						}
					}
				}

				console.log(`Purged:                  ${purgedFileSuccessCount}`);
				if (purgedFileFailedCount > 0) {
					console.warn(`Unable to purge:         ${purgedFileFailedCount}`);
				}
			}

			const endTime = new Date();
			const formattedEndTime = DateHelpers.format(endTime, DATE_TIME_FORMAT);
			const ellapsedMilliseconds = endTime.getTime() - startTime.getTime();
			console.log(`\nEnd Time:  ${formattedEndTime}  [Ellapased: ${ellapsedMilliseconds}ms]`);

		} catch (taskError) {
			console.error(taskError);
		}

		console.debug(`Exiting DeployFilesTask.runAsync().`);
	};

}

// Ok to cast these as string, we validate they're defined above
const deployFilesTask = new DeployFilesTask(destinationPath as string, purgeAfterCopy, renameLockedFiles, skipUnmodified, sourcePath as string);
deployFilesTask.runAsync()
	.then(() => {
		process.exit(0);
	})
	.catch(asyncError => {
		console.error('(DeployFilesTask) ERROR: ');
		console.error(getFormattedErrorMessage(asyncError));
		process.exit(1);
	});
