const fs = require('fs/promises');
const path = require('path');
const sharp = require('C:/Users/Windows 11/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root = 'C:/Users/Windows 11/juegopesca';
async function main() {
  const out = path.join(root, 'Docs/Presentacion/.build/decoracion');
  await fs.mkdir(out, { recursive: true });
  for (const name of ['Rama1','Rama2','Rama3','Rama4','Coral1','Coral2']) {
    const source = path.join(root,'Assets/Resources/DecoracionCatalogo',name+'.png');
    const {data,info} = await sharp(source).resize({width:900,withoutEnlargement:true}).ensureAlpha().raw().toBuffer({resolveWithObject:true});
    for(let i=3;i<data.length;i+=4) data[i] = Math.round(data[i]*0.16);
    await sharp(data,{raw:info}).resize({width:500}).webp({quality:80}).toFile(path.join(out,name+'.webp'));
  }
  for(const name of ['Pez_Vela','Mero_Guasa','Pez_Leon']) {
    await sharp(path.join(root,'Assets/Sprites/Peces/SanAndres',name+'.png')).resize({width:650}).webp({quality:85}).toFile(path.join(out,name+'.webp'));
  }
  console.log('Decoraciones y especies preparadas.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
