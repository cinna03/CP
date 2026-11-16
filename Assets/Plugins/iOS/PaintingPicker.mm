#import <PhotosUI/PhotosUI.h>
#import <UIKit/UIKit.h>

extern UIViewController *UnityGetGLViewController();
extern void UnitySendMessage(const char *objectName, const char *methodName, const char *message);

@interface CapstonePaintingPicker : NSObject <PHPickerViewControllerDelegate>
@property (nonatomic, copy) NSString *target;
@end

@implementation CapstonePaintingPicker

- (void)picker:(PHPickerViewController *)picker didFinishPicking:(NSArray<PHPickerResult *> *)results
{
    NSString *target = self.target != nil ? self.target : @"";
    if (results.count == 0)
    {
        [picker dismissViewControllerAnimated:YES completion:^{
            UnitySendMessage(target.UTF8String, "OnPaintingPicked", "");
        }];
        return;
    }

    NSItemProvider *provider = results[0].itemProvider;
    if (![provider canLoadObjectOfClass:[UIImage class]])
    {
        [picker dismissViewControllerAnimated:YES completion:^{
            UnitySendMessage(target.UTF8String, "OnPaintingPicked", "");
        }];
        return;
    }

    [provider loadObjectOfClass:[UIImage class] completionHandler:^(id<NSItemProviderReading> object, NSError *error) {
        UIImage *image = [object isKindOfClass:[UIImage class]] ? (UIImage *)object : nil;
        NSData *data = image != nil ? UIImageJPEGRepresentation(image, 0.85) : nil;
        NSString *path = [NSTemporaryDirectory() stringByAppendingPathComponent:@"capstone-painting.jpg"];
        BOOL wrote = data != nil && [data writeToFile:path atomically:YES];
        dispatch_async(dispatch_get_main_queue(), ^{
            [picker dismissViewControllerAnimated:YES completion:^{
                UnitySendMessage(target.UTF8String, "OnPaintingPicked", wrote ? path.UTF8String : "");
            }];
        });
    }];
}

@end

static CapstonePaintingPicker *capstonePicker;

extern "C" void CapstonePickPainting(const char *gameObjectName)
{
    if (capstonePicker == nil)
        capstonePicker = [CapstonePaintingPicker new];
    capstonePicker.target = gameObjectName != NULL ? [NSString stringWithUTF8String:gameObjectName] : @"";
    dispatch_async(dispatch_get_main_queue(), ^{
        PHPickerConfiguration *configuration = [PHPickerConfiguration new];
        configuration.filter = [PHPickerFilter imagesFilter];
        configuration.selectionLimit = 1;
        PHPickerViewController *picker = [[PHPickerViewController alloc] initWithConfiguration:configuration];
        picker.delegate = capstonePicker;
        [UnityGetGLViewController() presentViewController:picker animated:YES completion:nil];
    });
}
