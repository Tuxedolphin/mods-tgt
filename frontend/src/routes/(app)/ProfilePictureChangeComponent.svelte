<script lang="ts">
  import Cropper, { type CropArea } from "svelte-easy-crop";
  // taken from cropper documentation:
  // https://valentinh.github.io/react-easy-crop/docs/examples/output
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

    return new Promise<string | null>((resolve) => {
      croppedCanvas.toBlob((file) => {
        resolve(file ? URL.createObjectURL(file) : null);
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
  <p>Change your Photo!</p>
  <input
    type="file"
    accept="image/*"
    class="file-input"
    bind:files={image_files}
    onchange={async () => {
      if (image_files) {
        console.log(image_files[0].name);
        image = URL.createObjectURL(image_files[0]);
      }
    }}
  />
  {#if image}
    <div class="relative w-full h-80">
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
      class="btn btn-accent"
      onclick={async () => {
        const cropped_image = await getCroppedImg(image, crop_area);
        console.log(cropped_image);
      }}>Upload Image</button
    >
  {/if}
</div>
