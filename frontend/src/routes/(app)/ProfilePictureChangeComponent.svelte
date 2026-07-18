<script lang="ts">
  import Cropper, { type CropArea } from "svelte-easy-crop";
  import {
    currentUserInformation,
    token_information,
  } from "$lib/shared/shared.svelte";
  import {
    delete_user_profile_photo,
    get_user_info,
    update_user_profile_photo,
  } from "$lib/utils/db_operations";

  // taken from cropper documentation:
  // https://valentinh.github.io/react-easy-crop/docs/examples/output

  interface ProfilePictureChangeComponentProps {
    parentDialog: HTMLDialogElement;
  }

  let loading = $state(false);
  let error = $state("");

  let { parentDialog }: ProfilePictureChangeComponentProps = $props();

  let image_files: FileList | undefined = $state();

  let image = $state();
  let crop = $state({ x: 0, y: 0 });
  let zoom = $state(1);
  let crop_area: CropArea = $state({
    height: 0,
    width: 0,
    x: 0,
    y: 0,
  });
  async function getCroppedImg(
    imageSrc: string,
    pixelCrop: CropArea,
    rotation = 0,
    flip = { horizontal: false, vertical: false },
  ) {
    const image = await createImage(imageSrc);
    const canvas = document.createElement("canvas");
    const ctx = canvas.getContext("2d");

    if (!ctx) {
      return null;
    }

    const rotRad = getRadianAngle(rotation);
    const { width: bBoxWidth, height: bBoxHeight } = rotateSize(
      image.width,
      image.height,
      rotation,
    );

    canvas.width = bBoxWidth;
    canvas.height = bBoxHeight;

    ctx.translate(bBoxWidth / 2, bBoxHeight / 2);
    ctx.rotate(rotRad);
    ctx.scale(flip.horizontal ? -1 : 1, flip.vertical ? -1 : 1);
    ctx.translate(-image.width / 2, -image.height / 2);
    ctx.drawImage(image, 0, 0);

    const croppedCanvas = document.createElement("canvas");
    const croppedCtx = croppedCanvas.getContext("2d");

    if (!croppedCtx) {
      return null;
    }

    croppedCanvas.width = pixelCrop.width;
    croppedCanvas.height = pixelCrop.height;

    croppedCtx.drawImage(
      canvas,
      pixelCrop.x,
      pixelCrop.y,
      pixelCrop.width,
      pixelCrop.height,
      0,
      0,
      pixelCrop.width,
      pixelCrop.height,
    );

    return new Promise<Blob | null>((resolve) => {
      croppedCanvas.toBlob((file) => {
        resolve(file ? file : null);
      }, "image/jpeg");
    });
  }

  function getRadianAngle(degreeValue: number) {
    return (degreeValue * Math.PI) / 180;
  }

  function rotateSize(width: number, height: number, rotation: number) {
    const rotRad = getRadianAngle(rotation);

    return {
      width:
        Math.abs(Math.cos(rotRad) * width) +
        Math.abs(Math.sin(rotRad) * height),
      height:
        Math.abs(Math.sin(rotRad) * width) +
        Math.abs(Math.cos(rotRad) * height),
    };
  }
  function createImage(url: string) {
    return new Promise<HTMLImageElement>((resolve, reject) => {
      const image = new Image();
      image.addEventListener("load", () => resolve(image));
      image.addEventListener("error", reject);
      image.setAttribute("crossOrigin", "anonymous");
      image.src = url;
    });
  }
</script>

<div class="flex flex-col">
  <p class="text-lg font-bold mb-1">Change your Photo!</p>

  <p class="text-error">{error}</p>
  <div>
    <input
      type="file"
      accept="image/png, image/jpeg, .png, .jpg, .jpeg"
      class="file-input w-full"
      bind:files={image_files}
      onchange={async () => {
        if (image_files) {
          image = URL.createObjectURL(image_files[0]);
        }
      }}
    />
    {#if image}
      <div class="relative w-full h-80 my-1">
        <Cropper
          {image}
          oncropcomplete={(e) => {
            crop_area = e.pixels;
          }}
          bind:crop
          bind:zoom
          aspect={1}
        ></Cropper>
      </div>

      <button
        class="btn btn-accent my-1 w-full {loading ? 'btn-disabled' : ''}"
        onclick={async () => {
          error = "";
          loading = true;
          const cropped_image = await getCroppedImg(image, crop_area);
          const result = await update_user_profile_photo(
            cropped_image!,
            $token_information.a,
          );

          const new_user_info = await get_user_info($token_information.a);

          if (result.isOk()) {
            image = "";
            crop_area = {
              height: 0,
              width: 0,
              x: 0,
              y: 0,
            };
            image_files = undefined;
          } else {
            error = result.error;
            loading = false;
            return;
          }

          if (new_user_info.isOk()) {
            currentUserInformation.set(new_user_info.value);
          }
          loading = false;
          parentDialog.close();
        }}>Upload Image</button
      >
    {/if}
  </div>

  <div class="divider"></div>

  <button
    class="btn btn-error {loading ? 'btn-disabled' : ''}"
    onclick={async () => {
      loading = true;
      error = "";
      const delete_request = await delete_user_profile_photo(
        $token_information.a,
      );

      if (delete_request.isErr()) {
        loading = false;
        error = delete_request.error;
        return;
      }

      const new_user_info = await get_user_info($token_information.a);

      if (new_user_info.isErr()) {
        loading = false;
        error = new_user_info.error;
        return;
      }

      currentUserInformation.set(new_user_info.value);
      loading = false;
      parentDialog.close();
    }}>Remove Profile Picture</button
  >
</div>
